namespace LaMeridienneAPI.Models;

/// <summary>Quantité vendue d'un journal sur une feuille ; écrite par une requête atomique (R-09).</summary>
public class NewspaperSale : AuditableEntity
{
    public const int MaxQuantity = 999;

    public Guid SheetId { get; set; }
    public Guid NewspaperId { get; set; }
    public int Quantity { get; set; }
}
