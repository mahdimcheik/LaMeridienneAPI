using LaMeridienneAPI.Data;
using LaMeridienneAPI.Dtos;
using LaMeridienneAPI.Infrastructure;
using LaMeridienneAPI.Models;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace LaMeridienneAPI.Services;

/// <summary>Référentiel clients (R-08) : recherche, résolution d'une saisie, crédits, renommage et fusion.</summary>
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

        var escaped = EscapeLike(term);
        return await db.Customers.AsNoTracking()
            .Where(c => c.MergedIntoId == null)
            .Where(c => EF.Functions.Like(c.NormalizedName, "%" + escaped + "%", @"\"))
            .OrderBy(c => !EF.Functions.Like(c.NormalizedName, escaped + "%", @"\"))
            .ThenBy(c => c.NormalizedName)
            .Take(limit ?? SearchMaxResults)
            .Select(c => new CustomerResponse(c.Id, c.Name, c.Phone))
            .ToListAsync(ct);
    }

    public async Task<CustomerCreditsResponse> GetCreditsAsync(Guid id, CancellationToken ct)
    {
        var customer = await db.Customers.AsNoTracking().SingleOrDefaultAsync(c => c.Id == id && c.MergedIntoId == null, ct)
                       ?? throw UnknownCustomer();
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
            return await db.Customers.SingleOrDefaultAsync(c => c.Id == id && c.MergedIntoId == null, ct)
                   ?? throw new FieldValidationException("customerId", "Ce client n'existe pas.");
        if (clean.Length == 0)
            throw new FieldValidationException("customerName", "Saisissez un client.");
        if (clean.Length > Customer.NameMaxLength)
            throw new FieldValidationException("customerName", "Le nom du client fait au plus 100 caractères.");

        var normalized = CustomerNames.Normalize(clean);
        var existing = await db.Customers.SingleOrDefaultAsync(c => c.NormalizedName == normalized && c.MergedIntoId == null, ct);
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
            return await db.Customers.SingleAsync(c => c.NormalizedName == normalized && c.MergedIntoId == null, ct);
        }
    }

    public const int ListMaxPageSize = 100;

    /// <summary>Liste paginée du référentiel (US-E08-02), par nom ; <paramref name="search"/> filtre par « contient ».</summary>
    public async Task<CustomerPageResponse> ListAsync(string? search, int? page, int? pageSize, CancellationToken ct)
    {
        if (page is < 1)
            throw new FieldValidationException("page", "La page commence à 1.");
        if (pageSize is < 1 or > ListMaxPageSize)
            throw new FieldValidationException("pageSize", "La taille de page est comprise entre 1 et 100.");
        var size = pageSize ?? 50;

        var query = db.Customers.AsNoTracking().Where(c => c.MergedIntoId == null);
        var term = CustomerNames.Normalize(search);
        if (term.Length > 0)
        {
            var escaped = EscapeLike(term);
            query = query.Where(c => EF.Functions.Like(c.NormalizedName, "%" + escaped + "%", @"\"));
        }

        var total = await query.CountAsync(ct);
        var items = await query.OrderBy(c => c.NormalizedName)
            .Skip(((page ?? 1) - 1) * size).Take(size)
            .Select(c => new CustomerResponse(c.Id, c.Name, c.Phone))
            .ToListAsync(ct);
        return new CustomerPageResponse(items, total);
    }

    /// <summary>
    /// Renommer touche toutes les lignes du client, qui ne portent que son identifiant. Un nom déjà
    /// porté par un autre client répond <c>409</c> avec ce client, pour proposer la fusion.
    /// </summary>
    public async Task<CustomerResponse> UpdateAsync(Guid id, CustomerUpdateRequest request, CancellationToken ct)
    {
        var customer = await db.Customers.SingleOrDefaultAsync(c => c.Id == id && c.MergedIntoId == null, ct)
                       ?? throw UnknownCustomer();
        var clean = CustomerNames.Clean(request.Name);
        if (clean.Length == 0)
            throw new FieldValidationException("name", "Saisissez le nom du client.");
        if (clean.Length > Customer.NameMaxLength)
            throw new FieldValidationException("name", "Le nom du client fait au plus 100 caractères.");

        var normalized = CustomerNames.Normalize(clean);
        var other = await db.Customers.AsNoTracking()
            .SingleOrDefaultAsync(c => c.NormalizedName == normalized && c.MergedIntoId == null && c.Id != id, ct);
        if (other is not null)
            throw NameTaken(other);

        customer.Name = clean;
        customer.NormalizedName = normalized;
        var phone = request.Phone?.Trim();
        customer.Phone = string.IsNullOrEmpty(phone) ? null : phone;
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException e) when (e.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            throw NameTaken(await db.Customers.AsNoTracking()
                .SingleAsync(c => c.NormalizedName == normalized && c.MergedIntoId == null && c.Id != id, ct));
        }
        return CustomerResponse.From(customer);
    }

    /// <summary>
    /// Fusionne <paramref name="sourceId"/> dans <paramref name="targetId"/> : toutes les lignes et tous
    /// les rendez-vous, même supprimés, passent à la cible ; la source n'est plus proposée (US-E08-02).
    /// </summary>
    public async Task<CustomerResponse> MergeAsync(Guid targetId, Guid sourceId, CancellationToken ct)
    {
        if (targetId == sourceId)
            throw new FieldValidationException("sourceId", "Un client ne peut pas être fusionné avec lui-même.");

        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        var both = await db.Customers
            .FromSql($"""SELECT * FROM "Customers" WHERE "Id" IN ({targetId}, {sourceId}) FOR UPDATE""")
            .Where(c => c.MergedIntoId == null)
            .ToListAsync(ct);
        var target = both.SingleOrDefault(c => c.Id == targetId) ?? throw UnknownCustomer();
        var source = both.SingleOrDefault(c => c.Id == sourceId)
                     ?? throw new NotFoundException("Le client à fusionner n'existe pas ou a déjà été fusionné.");

        await db.CreditEntries.IgnoreQueryFilters().Where(e => e.CustomerId == sourceId)
            .ExecuteUpdateAsync(u => u.SetProperty(e => e.CustomerId, targetId), ct);
        await db.RefundEntries.IgnoreQueryFilters().Where(e => e.CustomerId == sourceId)
            .ExecuteUpdateAsync(u => u.SetProperty(e => e.CustomerId, targetId), ct);
        await db.TireAppointments.IgnoreQueryFilters().Where(a => a.CustomerId == sourceId)
            .ExecuteUpdateAsync(u => u.SetProperty(a => a.CustomerId, targetId), ct);

        target.Phone ??= source.Phone;
        source.MergedIntoId = targetId;
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return CustomerResponse.From(target);
    }

    private static string EscapeLike(string term) =>
        term.Replace(@"\", @"\\").Replace("%", @"\%").Replace("_", @"\_");

    private static NotFoundException UnknownCustomer() => new("Ce client n'existe pas.");

    private static ConflictException NameTaken(Customer other) => new(
        "Un autre client porte déjà ce nom : fusionnez les deux fiches.",
        new Dictionary<string, object?> { ["conflictingCustomer"] = CustomerResponse.From(other) });
}
