namespace LaMeridienneAPI.Models;

/// <summary>Rendez-vous de l'agenda des pneus (R-12), indépendant des feuilles du jour.</summary>
public class TireAppointment : AuditableEntity, ISoftDeletable
{
    public static readonly TimeSpan Step = TimeSpan.FromMinutes(15);
    public static readonly TimeSpan MinDuration = TimeSpan.FromMinutes(15);
    public static readonly TimeSpan MaxDuration = TimeSpan.FromHours(8);
    public const int PhoneMaxLength = 30;
    public const int VehicleMaxLength = 100;
    public const int ServiceMaxLength = 200;
    public const int NoteMaxLength = 1000;

    public Guid Id { get; set; } = Guid.CreateVersion7();
    public DateTimeOffset Start { get; set; }
    public DateTimeOffset End { get; set; }
    public Guid CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;
    public string? Phone { get; set; }
    public string? Vehicle { get; set; }
    public string? Service { get; set; }
    public string? Note { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
}
