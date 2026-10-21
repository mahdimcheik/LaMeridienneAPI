using LaMeridienneAPI.Data;
using LaMeridienneAPI.Dtos;
using LaMeridienneAPI.Infrastructure;
using LaMeridienneAPI.Models;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace LaMeridienneAPI.Services;

/// <summary>Référentiel clients (R-08) : recherche, résolution d'une saisie, crédits d'un client.</summary>
public sealed class CustomerService(AppDbContext db)
{
    public const int SearchMinLength = 2;
    public const int SearchMaxResults = 10;

    /// <summary>Noms qui <b>contiennent</b> le terme, sans casse ni accents ; ceux qui commencent par lui d'abord.</summary>
    public async Task<IReadOnlyList<CustomerResponse>> SearchAsync(string? search, int? limit, CancellationToken ct)
    {
        var term = CustomerNames.Normalize(search);
        if (term.Length < SearchMinLength)
            throw new FieldValidationException("search", "Saisissez au moins 2 caractères.");
        if (limit is < 1 or > SearchMaxResults)
            throw new FieldValidationException("limit", "Le nombre de résultats est compris entre 1 et 10.");

        var escaped = term.Replace(@"\", @"\\").Replace("%", @"\%").Replace("_", @"\_");
        return await db.Customers.AsNoTracking()
            .Where(c => EF.Functions.Like(c.NormalizedName, "%" + escaped + "%", @"\"))
            .OrderBy(c => !EF.Functions.Like(c.NormalizedName, escaped + "%", @"\"))
            .ThenBy(c => c.NormalizedName)
            .Take(limit ?? SearchMaxResults)
            .Select(c => new CustomerResponse(c.Id, c.Name, c.Phone))
            .ToListAsync(ct);
    }

    public async Task<CustomerCreditsResponse> GetCreditsAsync(Guid id, CancellationToken ct)
    {
        var customer = await db.Customers.AsNoTracking().SingleOrDefaultAsync(c => c.Id == id, ct)
                       ?? throw new NotFoundException("Ce client n'existe pas.");
        var credits = await db.CreditEntries.Where(e => e.CustomerId == id)
            .OrderByDescending(e => e.Sheet.Date).ThenByDescending(e => e.CreatedAt)
            .ToResponses(db)
            .ToListAsync(ct);
        return new CustomerCreditsResponse(CustomerResponse.From(customer), credits);
    }

    /// <summary>Le client désigné par une saisie : par identifiant, ou par nom (réutilisé ou créé).</summary>
    public async Task<Customer> ResolveAsync(Guid? id, string? name, CancellationToken ct)
    {
        var clean = CustomerNames.Clean(name);
        if (id is not null && clean.Length > 0)
            throw new FieldValidationException("customerName", "Désignez le client par sa fiche ou par son nom, pas les deux.");
        if (id is not null)
            return await db.Customers.SingleOrDefaultAsync(c => c.Id == id, ct)
                   ?? throw new FieldValidationException("customerId", "Ce client n'existe pas.");
        if (clean.Length == 0)
            throw new FieldValidationException("customerName", "Saisissez un client.");
        if (clean.Length > Customer.NameMaxLength)
            throw new FieldValidationException("customerName", "Le nom du client fait au plus 100 caractères.");

        var normalized = CustomerNames.Normalize(clean);
        var existing = await db.Customers.SingleOrDefaultAsync(c => c.NormalizedName == normalized, ct);
        if (existing is not null) return existing;

        var created = new Customer { Name = clean, NormalizedName = normalized };
        db.Customers.Add(created);
        try
        {
            await db.SaveChangesAsync(ct);
            return created;
        }
        catch (DbUpdateException e) when (e.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            db.Entry(created).State = EntityState.Detached;
            return await db.Customers.SingleAsync(c => c.NormalizedName == normalized, ct);
        }
    }
}
