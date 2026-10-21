using System.Security.Claims;

namespace LaMeridienneAPI.Auth;

public interface ICurrentUser
{
    Guid? UserId { get; }
}

public sealed class HttpCurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    public Guid? UserId =>
        Guid.TryParse(accessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier)
                      ?? accessor.HttpContext?.User.FindFirstValue("sub"), out var id)
            ? id
            : null;
}
