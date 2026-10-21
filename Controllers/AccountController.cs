using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using LaMeridienneAPI.Auth;
using Microsoft.AspNetCore.Mvc;

namespace LaMeridienneAPI.Controllers;

public sealed record ChangePasswordRequest
{
    [Required(ErrorMessage = "Saisissez votre mot de passe actuel.")]
    public string CurrentPassword { get; init; } = "";

    [Required(ErrorMessage = "Saisissez un nouveau mot de passe.")]
    public string NewPassword { get; init; } = "";
}

[ApiController]
[Route("api/account")]
public sealed class AccountController(AuthService auth, ICurrentUser currentUser) : ControllerBase
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
}
