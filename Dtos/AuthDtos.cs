using System.ComponentModel.DataAnnotations;
using LaMeridienneAPI.Models;

namespace LaMeridienneAPI.Dtos;

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

public sealed record ChangePasswordRequest
{
    [Required(ErrorMessage = "Saisissez votre mot de passe actuel.")]
    public string CurrentPassword { get; init; } = "";

    [Required(ErrorMessage = "Saisissez un nouveau mot de passe.")]
    public string NewPassword { get; init; } = "";
}
