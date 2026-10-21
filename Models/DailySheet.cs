namespace LaMeridienneAPI.Models;

/// <summary>Feuille d'une date calendaire de la boutique (R-01, ADR-0004).</summary>
public class DailySheet : AuditableEntity
{
    public const int NotesMaxLength = 10_000;

    public Guid Id { get; set; } = Guid.CreateVersion7();
    public DateOnly Date { get; set; }
    public string Notes { get; set; } = "";

    /// <summary>Jeton de concurrence des notes (R-11) : incrémenté à chaque enregistrement.</summary>
    public int NotesVersion { get; set; }
}
