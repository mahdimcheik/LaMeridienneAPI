using LaMeridienneAPI.Models;

namespace LaMeridienneAPI.Dtos;

public sealed record CustomerResponse(Guid Id, string Name, string? Phone)
{
    public static CustomerResponse From(Customer c) => new(c.Id, c.Name, c.Phone);
}

/// <summary>Lignes de crédit d'un client, de la plus récente à la plus ancienne ; aucun total (R-04).</summary>
public sealed record CustomerCreditsResponse(CustomerResponse Customer, IReadOnlyList<EntryResponse> Credits);
