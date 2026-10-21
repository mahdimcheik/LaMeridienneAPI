namespace LaMeridienneAPI.Models;

/// <summary>Suppression logique (R-06) : filtrée de toutes les lectures par <see cref="Data.AppDbContext"/>.</summary>
public interface ISoftDeletable
{
    DateTimeOffset? DeletedAt { get; set; }
}
