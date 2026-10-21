namespace LaMeridienneAPI.Models;

/// <summary>Référentiel clients (R-08) partagé par crédits, remboursements et rendez-vous.</summary>
public class Customer
{
    public const int NameMaxLength = 100;

    public Guid Id { get; set; } = Guid.CreateVersion7();
    public string Name { get; set; } = "";

    /// <summary>Nom sans casse ni accents, unique : voir <see cref="Services.CustomerNames"/>.</summary>
    public string NormalizedName { get; set; } = "";

    public string? Phone { get; set; }
}
