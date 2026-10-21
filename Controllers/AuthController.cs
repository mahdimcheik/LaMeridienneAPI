using System.ComponentModel.DataAnnotations;
using LaMeridienneAPI.Auth;
using LaMeridienneAPI.Data;
using LaMeridienneAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace LaMeridienneAPI.Controllers;

public sealed record LoginRequest
{
    [Required(ErrorMessage = "Saisissez votre identifiant.")]
    public string UserName { get; init; } = "";

    [Required(ErrorMessage = "Saisissez votre mot de passe.")]
    public string Password { get; init; } = "";
}

public sealed record CurrentUserResponse(Guid Id, string UserName, string DisplayName)
{
    public static CurrentUserResponse From(User user) => new(user.Id, user.UserName, user.DisplayName);
}

public sealed record AuthResponse(string AccessToken, DateTimeOffset ExpiresAt, CurrentUserResponse User);

/// <summary>Session (E01) : jeton d'accès dans le corps, jeton de renouvellement en cookie uniquement.</summary>
[ApiController]
[Route("api/auth")]
public sealed class AuthController(AuthService auth, ICurrentUser currentUser, AppDbContext db) : ControllerBase
{
    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting(AuthSetup.AuthRateLimit)]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request, CancellationToken ct) =>
        await auth.LoginAsync(request.UserName, request.Password, ct) switch
        {
            LoginResult.Success s => Issue(s.Session),
            LoginResult.Locked => Problem(statusCode: StatusCodes.Status423Locked,
                detail: $"Trop d'échecs de connexion : compte verrouillé. Réessayez dans {AuthService.LockoutDuration.TotalMinutes} minutes."),
            _ => Problem(statusCode: StatusCodes.Status401Unauthorized, detail: "Identifiant ou mot de passe incorrect."),
        };

    [HttpPost("refresh")]
    [AllowAnonymous]
    [EnableRateLimiting(AuthSetup.AuthRateLimit)]
    public async Task<ActionResult<AuthResponse>> Refresh(CancellationToken ct)
    {
        var token = Request.Cookies[TokenService.CookieName];
        var session = string.IsNullOrEmpty(token) ? null : await auth.RefreshAsync(token, ct);
        if (session is not null) return Issue(session);

        TokenService.ClearCookie(Response);
        return Problem(statusCode: StatusCodes.Status401Unauthorized, detail: "Session expirée. Reconnectez-vous.");
    }

    /// <summary>Anonyme : détenir le cookie suffit pour le révoquer, même si le jeton d'accès a expiré.</summary>
    [HttpPost("logout")]
    [AllowAnonymous]
    public async Task<IActionResult> Logout(CancellationToken ct)
    {
        var token = Request.Cookies[TokenService.CookieName];
        if (!string.IsNullOrEmpty(token))
            await auth.LogoutAsync(token, ct);
        TokenService.ClearCookie(Response);
        return NoContent();
    }

    [HttpGet("me")]
    public async Task<ActionResult<CurrentUserResponse>> Me(CancellationToken ct)
    {
        var user = await db.Users.AsNoTracking().SingleOrDefaultAsync(u => u.Id == currentUser.UserId, ct);
        return user is null ? Unauthorized() : CurrentUserResponse.From(user);
    }

    private AuthResponse Issue(AuthSession session)
    {
        TokenService.WriteCookie(Response, session.RefreshToken, session.RefreshExpiresAt);
        return new AuthResponse(session.AccessToken, session.AccessExpiresAt, CurrentUserResponse.From(session.User));
    }
}
