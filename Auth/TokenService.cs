using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;
using LaMeridienneAPI.Models;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace LaMeridienneAPI.Auth;

/// <summary>Émission des jetons et du cookie de renouvellement (ADR-0002, sécurité).</summary>
public sealed class TokenService(IOptions<JwtOptions> options, TimeProvider time)
{
    public const string CookieName = "lm_refresh";
    public const string CookiePath = "/api/auth";
    public const string SessionClaim = "sid";
    public const string AdminRole = "Admin";
    public static readonly TimeSpan AccessLifetime = TimeSpan.FromMinutes(15);
    public static readonly TimeSpan RefreshLifetime = TimeSpan.FromDays(30);

    private readonly JsonWebTokenHandler _handler = new();

    public static SymmetricSecurityKey SigningKey(JwtOptions jwt) => new(Encoding.UTF8.GetBytes(jwt.Key));

    /// <param name="sessionId">Identifiant du jeton de renouvellement de cette session, porté en claim <c>sid</c>.</param>
    public (string Token, DateTimeOffset ExpiresAt) CreateAccessToken(User user, Guid sessionId)
    {
        var jwt = options.Value;
        var now = time.GetUtcNow();
        var expiresAt = now + AccessLifetime;
        var token = _handler.CreateToken(new SecurityTokenDescriptor
        {
            Issuer = jwt.Issuer,
            Audience = jwt.Audience,
            IssuedAt = now.UtcDateTime,
            NotBefore = now.UtcDateTime,
            Expires = expiresAt.UtcDateTime,
            SigningCredentials = new SigningCredentials(SigningKey(jwt), SecurityAlgorithms.HmacSha256),
            Claims = new Dictionary<string, object>
            {
                [JwtRegisteredClaimNames.Sub] = user.Id.ToString(),
                [JwtRegisteredClaimNames.Name] = user.DisplayName,
                ["role"] = AdminRole,
                [JwtRegisteredClaimNames.Jti] = Guid.NewGuid().ToString(),
                [SessionClaim] = sessionId.ToString(),
            },
        });
        return (token, expiresAt);
    }

    public static string NewRefreshToken() => Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(32));

    public static string Hash(string refreshToken) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken)));

    public static void WriteCookie(HttpResponse response, string refreshToken, DateTimeOffset expiresAt) =>
        response.Cookies.Append(CookieName, refreshToken, CookieOptions(expiresAt));

    public static void ClearCookie(HttpResponse response) =>
        response.Cookies.Delete(CookieName, CookieOptions(null));

    private static CookieOptions CookieOptions(DateTimeOffset? expiresAt) => new()
    {
        HttpOnly = true,
        Secure = true,
        SameSite = SameSiteMode.Strict,
        Path = CookiePath,
        Expires = expiresAt,
        IsEssential = true,
    };
}
