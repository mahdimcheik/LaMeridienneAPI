namespace LaMeridienneAPI.Models;

/// <summary>Un des trois comptes pré-créés (ADR-0003) ; jamais créé ni supprimé par l'API.</summary>
public class User
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public string UserName { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public int FailedLoginCount { get; set; }
    public DateTimeOffset? LockedUntil { get; set; }
}
