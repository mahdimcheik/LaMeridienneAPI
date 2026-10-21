using System.ComponentModel.DataAnnotations;
using LaMeridienneAPI.Models;

namespace LaMeridienneAPI.Dtos;

public sealed record NewspaperResponse(Guid Id, string Name, bool IsActive, int SortOrder);

public sealed record NewspaperRequest
{
    [MaxLength(Newspaper.NameMaxLength, ErrorMessage = "Le nom du journal fait au plus 100 caractères.")]
    public string? Name { get; init; }

    /// <summary>Ignoré à la création : un journal ajouté est actif.</summary>
    public bool? IsActive { get; init; }
}

/// <summary>Tous les journaux, chacun une fois, dans l'ordre voulu.</summary>
public sealed record NewspaperOrderRequest
{
    [Required(ErrorMessage = "Donnez l'ordre des journaux.")]
    public IReadOnlyList<Guid>? Ids { get; init; }
}

/// <summary>Ligne de la case Journaux d'une feuille (R-10).</summary>
public sealed record NewspaperSaleResponse(Guid NewspaperId, string Name, bool IsActive, int Quantity);

public sealed record SaleQuantityRequest
{
    private const string OutOfRange = "La quantité est un nombre entier entre 0 et 999.";

    [Required(ErrorMessage = OutOfRange), Range(0, NewspaperSale.MaxQuantity, ErrorMessage = OutOfRange)]
    public int? Quantity { get; init; }
}

public sealed record SaleIncrementRequest
{
    private const string Invalid = "L'incrément vaut +1 ou −1.";

    [Required(ErrorMessage = Invalid), AllowedValues(1, -1, ErrorMessage = Invalid)]
    public int? Delta { get; init; }
}
