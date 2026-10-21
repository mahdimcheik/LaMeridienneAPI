using System.ComponentModel.DataAnnotations;
using LaMeridienneAPI.Models;

namespace LaMeridienneAPI.Dtos;

public sealed record CustomerResponse(Guid Id, string Name, string? Phone)
{
    public static CustomerResponse From(Customer c) => new(c.Id, c.Name, c.Phone);
}

/// <summary>Lignes de crédit d'un client, de la plus récente à la plus ancienne ; aucun total (R-04).</summary>
public sealed record CustomerCreditsResponse(CustomerResponse Customer, IReadOnlyList<EntryResponse> Credits);

public sealed record CustomerPageResponse(IReadOnlyList<CustomerResponse> Items, int Total);

public sealed record CustomerUpdateRequest
{
    [MaxLength(Customer.NameMaxLength, ErrorMessage = "Le nom du client fait au plus 100 caractères.")]
    public string? Name { get; init; }

    [MaxLength(30, ErrorMessage = "Le téléphone fait au plus 30 caractères.")]
    public string? Phone { get; init; }
}

/// <summary>La fiche <c>sourceId</c> est fusionnée dans celle de la route.</summary>
public sealed record CustomerMergeRequest
{
    [Required(ErrorMessage = "Désignez le client à fusionner.")]
    public Guid? SourceId { get; init; }
}
