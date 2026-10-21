using System.ComponentModel.DataAnnotations;
using LaMeridienneAPI.Infrastructure;
using LaMeridienneAPI.Models;

namespace LaMeridienneAPI.Dtos;

public sealed record CustomerRef(Guid Id, string Name);

/// <summary>Ligne de crédit ou de remboursement ; l'auteur est le nom affiché, posé par l'API (US-E09-03).</summary>
public sealed record EntryResponse(
    Guid Id,
    DateOnly Date,
    CustomerRef Customer,
    decimal Amount,
    string? Comment,
    string? CreatedBy,
    DateTimeOffset CreatedAt,
    string? UpdatedBy,
    DateTimeOffset? UpdatedAt);

/// <summary>Client désigné par <c>customerId</c> <b>ou</b> par <c>customerName</c> (créé à la volée, R-08).</summary>
public sealed record EntryRequest
{
    public Guid? CustomerId { get; init; }

    [MaxLength(Customer.NameMaxLength, ErrorMessage = "Le nom du client fait au plus 100 caractères.")]
    public string? CustomerName { get; init; }

    [Required(ErrorMessage = "Saisissez un montant.")]
    [Amount]
    public decimal? Amount { get; init; }

    [MaxLength(CustomerEntry.CommentMaxLength, ErrorMessage = "Le commentaire fait au plus 500 caractères.")]
    public string? Comment { get; init; }
}

public sealed record SheetResponse(
    DateOnly Date,
    bool Exists,
    string Notes,
    int NotesVersion,
    IReadOnlyList<EntryResponse> Credits,
    IReadOnlyList<EntryResponse> Refunds,
    IReadOnlyList<NewspaperSaleResponse> Newspapers);

public sealed record NotesRequest
{
    [MaxLength(DailySheet.NotesMaxLength, ErrorMessage = "Les notes font au plus 10 000 caractères.")]
    public string? Notes { get; init; }

    /// <summary>Version sur laquelle la modification se fonde (<c>0</c> pour une feuille encore vide).</summary>
    [Required(ErrorMessage = "La version des notes est obligatoire.")]
    [Range(0, int.MaxValue, ErrorMessage = "La version des notes est invalide.")]
    public int? Version { get; init; }
}

public sealed record NotesResponse(string Notes, int NotesVersion);
