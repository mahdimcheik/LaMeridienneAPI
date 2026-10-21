namespace LaMeridienneAPI.Models;

/// <summary>
/// Forme commune d'une ligne de crédit ou de remboursement. Les deux restent des tables
/// distinctes et indépendantes (R-04).
/// </summary>
public abstract class CustomerEntry : AuditableEntity, ISoftDeletable
{
    public const decimal MaxAmount = 10_000m;
    public const int CommentMaxLength = 500;

    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid SheetId { get; set; }
    public DailySheet Sheet { get; set; } = null!;
    public Guid CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;
    public decimal Amount { get; set; }
    public string? Comment { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
}

public class CreditEntry : CustomerEntry;

public class RefundEntry : CustomerEntry;
