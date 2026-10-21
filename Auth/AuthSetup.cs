using System.Threading.RateLimiting;
using LaMeridienneAPI.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace LaMeridienneAPI.Auth;

public static class AuthSetup
{
    public const string AdminPolicy = "Admin";
    public const string AuthRateLimit = "auth";
    public const string ClientCors = "client";

    public static IServiceCollection AddLaMeridienneAuth(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<JwtOptions>().BindConfiguration(JwtOptions.Section).ValidateOnStart();
        services.AddSingleton<IValidateOptions<JwtOptions>, JwtOptionsValidator>();
        services.AddSingleton<IPasswordHasher<User>, PasswordHasher<User>>();
        services.AddSingleton<TokenService>();
        services.AddScoped<AuthService>();
        services.AddHostedService<UserSeeder>();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtOptions>, TimeProvider>((o, jwt, time) =>
            {
                var skew = TimeSpan.FromSeconds(30);
                o.MapInboundClaims = false;
                o.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidIssuer = jwt.Value.Issuer,
                    ValidAudience = jwt.Value.Audience,
                    IssuerSigningKey = TokenService.SigningKey(jwt.Value),
                    ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
                    NameClaimType = "name",
                    RoleClaimType = "role",
                    ClockSkew = skew,
                    // L'horloge de l'application (TimeProvider), pas celle de la bibliothèque.
                    LifetimeValidator = (notBefore, expires, _, _) =>
                    {
                        var now = time.GetUtcNow().UtcDateTime;
                        return expires is not null && now < expires.Value + skew
                               && (notBefore is null || now >= notBefore.Value - skew);
                    },
                };
            });

        services.AddAuthorizationBuilder()
            .AddPolicy(AdminPolicy, p => p.RequireAuthenticatedUser().RequireRole(TokenService.AdminRole));

        services.AddRateLimiter(o =>
        {
            o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            o.AddPolicy(AuthRateLimit, http => RateLimitPartition.GetFixedWindowLimiter(
                http.Connection.RemoteIpAddress?.ToString() ?? "inconnue",
                _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(1) }));
            o.OnRejected = (ctx, _) =>
            {
                if (ctx.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                    ctx.HttpContext.Response.Headers.RetryAfter = ((int)Math.Ceiling(retryAfter.TotalSeconds)).ToString();
                return ValueTask.CompletedTask;
            };
        });

        var origins = configuration.GetSection("Cors:Origins").Get<string[]>() ?? [];
        services.AddCors(o => o.AddPolicy(ClientCors, p => p
            .WithOrigins(origins).AllowAnyHeader().AllowAnyMethod().AllowCredentials()));

        return services;
    }
}
