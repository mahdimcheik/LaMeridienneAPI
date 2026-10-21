using System.Security.Claims;
using LaMeridienneAPI.Auth;
using LaMeridienneAPI.Dtos;
using LaMeridienneAPI.Services;
using Microsoft.AspNetCore.Mvc;

namespace LaMeridienneAPI.Controllers;

[ApiController]
[Route("api/account")]
public sealed class AccountController(AuthService auth, LayoutService layouts, ICurrentUser currentUser) : ControllerBase
{
    /// <summary>US-E08-03 : les autres sessions du compte sont révoquées, la session courante reste ouverte.</summary>
    [HttpPut("password")]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequest request, CancellationToken ct)
    {
        Guid? session = Guid.TryParse(User.FindFirstValue(TokenService.SessionClaim), out var sid) ? sid : null;
        var errors = await auth.ChangePasswordAsync(
            currentUser.UserId!.Value, session, request.CurrentPassword, request.NewPassword, ct);
        if (errors is null) return NoContent();

        foreach (var (field, message) in errors)
            ModelState.AddModelError(field, message);
        return ValidationProblem();
    }

    /// <summary>Ma disposition des cases (R-07), ou la disposition par défaut (<c>isDefault</c>).</summary>
    [HttpGet("layout")]
    public Task<LayoutDto> GetLayout(CancellationToken ct) => layouts.GetAsync(currentUser.UserId!.Value, ct);

    [HttpPut("layout")]
    public Task<LayoutDto> SaveLayout(LayoutDto layout, CancellationToken ct) =>
        layouts.SaveAsync(currentUser.UserId!.Value, layout, ct);
}
