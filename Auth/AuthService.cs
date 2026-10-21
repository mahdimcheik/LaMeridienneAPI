using LaMeridienneAPI.Data;
using LaMeridienneAPI.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace LaMeridienneAPI.Auth;

public sealed record AuthSession(
    User User, string AccessToken, DateTimeOffset AccessExpiresAt, string RefreshToken, DateTimeOffset RefreshExpiresAt);

public abstract record LoginResult
{
    public sealed record Success(AuthSession Session) : LoginResult;
    public sealed record Invalid : LoginResult;
    public sealed record Locked(DateTimeOffset Until) : LoginResult;
}

/// <summary>Règles de session : verrouillage (R-02), rotation et réutilisation (R-03), mot de passe (R-14).</summary>
public sealed class AuthService(AppDbContext db, TokenService tokens, IPasswordHasher<User> hasher, TimeProvider time)
{
    public const int MaxFailedLogins = 5;
    public const int MinPasswordLength = 12;
    public static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

    private static readonly User TimingDummy = new();
    private static readonly string TimingDummyHash = new PasswordHasher<User>().HashPassword(TimingDummy, "mot de passe factice");

    public async Task<LoginResult> LoginAsync(string userName, string password, CancellationToken ct)
    {
        var user = await db.Users.SingleOrDefaultAsync(u => u.UserName == userName.Trim(), ct);
        if (user is null)
        {
            hasher.VerifyHashedPassword(TimingDummy, TimingDummyHash, password);
            return new LoginResult.Invalid();
        }

        var now = time.GetUtcNow();
        if (user.LockedUntil > now)
            return new LoginResult.Locked(user.LockedUntil.Value);

        var verification = hasher.VerifyHashedPassword(user, user.PasswordHash, password);
        if (verification == PasswordVerificationResult.Failed)
        {
            user.FailedLoginCount++;
            if (user.FailedLoginCount >= MaxFailedLogins)
            {
                user.FailedLoginCount = 0;
                user.LockedUntil = now + LockoutDuration;
            }
            await db.SaveChangesAsync(ct);
            return new LoginResult.Invalid();
        }

        if (verification == PasswordVerificationResult.SuccessRehashNeeded)
            user.PasswordHash = hasher.HashPassword(user, password);
        user.FailedLoginCount = 0;
        user.LockedUntil = null;
        var (stored, refreshToken) = NewRefreshToken(user.Id, now);
        db.RefreshTokens.Add(stored);
        await db.SaveChangesAsync(ct);
        return new LoginResult.Success(Session(user, stored, refreshToken));
    }

    /// <returns>La nouvelle session, ou <c>null</c> si le jeton est inconnu, expiré, révoqué ou réutilisé.</returns>
    public async Task<AuthSession?> RefreshAsync(string refreshToken, CancellationToken ct)
    {
        var hash = TokenService.Hash(refreshToken);
        var current = await db.RefreshTokens.AsNoTracking().Include(t => t.User)
            .SingleOrDefaultAsync(t => t.TokenHash == hash, ct);
        if (current is null) return null;

        var now = time.GetUtcNow();
        if (current.ConsumedAt is not null)
        {
            await RevokeSessionsAsync(current.UserId, except: null, now, ct);
            return null;
        }
        if (current.RevokedAt is not null || current.ExpiresAt <= now) return null;

        var (next, nextToken) = NewRefreshToken(current.UserId, now);
        var consumed = await db.RefreshTokens
            .Where(t => t.Id == current.Id && t.ConsumedAt == null && t.RevokedAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.ConsumedAt, now).SetProperty(t => t.ReplacedById, next.Id), ct);
        if (consumed == 0)
        {
            await RevokeSessionsAsync(current.UserId, except: null, now, ct);
            return null;
        }

        db.RefreshTokens.Add(next);
        await db.SaveChangesAsync(ct);
        return Session(current.User, next, nextToken);
    }

    public async Task LogoutAsync(string refreshToken, CancellationToken ct)
    {
        var hash = TokenService.Hash(refreshToken);
        var now = time.GetUtcNow();
        await db.RefreshTokens
            .Where(t => t.TokenHash == hash && t.ConsumedAt == null && t.RevokedAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.RevokedAt, now), ct);
    }

    /// <returns>Les erreurs par champ (clés du corps JSON), ou <c>null</c> si le mot de passe a été changé.</returns>
    public async Task<Dictionary<string, string>?> ChangePasswordAsync(
        Guid userId, Guid? currentSession, string currentPassword, string newPassword, CancellationToken ct)
    {
        var user = await db.Users.SingleAsync(u => u.Id == userId, ct);

        var errors = new Dictionary<string, string>();
        if (hasher.VerifyHashedPassword(user, user.PasswordHash, currentPassword) == PasswordVerificationResult.Failed)
            errors["currentPassword"] = "Le mot de passe actuel est incorrect.";
        if (newPassword.Length < MinPasswordLength)
            errors["newPassword"] = $"Le nouveau mot de passe doit contenir au moins {MinPasswordLength} caractères.";
        if (errors.Count > 0) return errors;

        user.PasswordHash = hasher.HashPassword(user, newPassword);
        await db.SaveChangesAsync(ct);
        await RevokeSessionsAsync(userId, currentSession, time.GetUtcNow(), ct);
        return null;
    }

    private Task RevokeSessionsAsync(Guid userId, Guid? except, DateTimeOffset now, CancellationToken ct) =>
        db.RefreshTokens
            .Where(t => t.UserId == userId && t.ConsumedAt == null && t.RevokedAt == null && t.Id != except)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.RevokedAt, now), ct);

    private static (RefreshToken Stored, string Token) NewRefreshToken(Guid userId, DateTimeOffset now)
    {
        var token = TokenService.NewRefreshToken();
        return (new RefreshToken
        {
            UserId = userId,
            TokenHash = TokenService.Hash(token),
            CreatedAt = now,
            ExpiresAt = now + TokenService.RefreshLifetime,
        }, token);
    }

    private AuthSession Session(User user, RefreshToken stored, string refreshToken)
    {
        var (access, accessExpiresAt) = tokens.CreateAccessToken(user, stored.Id);
        return new AuthSession(user, access, accessExpiresAt, refreshToken, stored.ExpiresAt);
    }
}
