using LaMeridienneAPI.Auth;
using LaMeridienneAPI.Data;
using LaMeridienneAPI.Dtos;
using LaMeridienneAPI.Infrastructure;
using LaMeridienneAPI.Models;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace LaMeridienneAPI.Services;

/// <summary>Liste des journaux (US-E08-01) et quantités vendues par feuille (E05, R-09, R-10).</summary>
public sealed class NewspaperService(AppDbContext db, SheetService sheets, TimeProvider time, ICurrentUser currentUser)
{
    public Task<List<NewspaperResponse>> ListAsync(CancellationToken ct) =>
        db.Newspapers.AsNoTracking().OrderBy(n => n.SortOrder)
            .Select(n => new NewspaperResponse(n.Id, n.Name, n.IsActive, n.SortOrder))
            .ToListAsync(ct);

    /// <summary>Un journal ajouté est actif et se place en dernier.</summary>
    public async Task<NewspaperResponse> CreateAsync(string? name, CancellationToken ct)
    {
        var paper = new Newspaper { SortOrder = (await db.Newspapers.MaxAsync(n => (int?)n.SortOrder, ct) ?? 0) + 1 };
        await RenameAsync(paper, name, ct);
        db.Newspapers.Add(paper);
        await SaveNameAsync(ct);
        return From(paper);
    }

    /// <summary>Renommer s'applique aux feuilles passées : elles ne portent que l'identifiant.</summary>
    public async Task<NewspaperResponse> UpdateAsync(Guid id, NewspaperRequest request, CancellationToken ct)
    {
        var paper = await FindAsync(id, ct);
        await RenameAsync(paper, request.Name, ct);
        if (request.IsActive is { } active) paper.IsActive = active;
        await SaveNameAsync(ct);
        return From(paper);
    }

    public async Task<List<NewspaperResponse>> ReorderAsync(IReadOnlyList<Guid> ids, CancellationToken ct)
    {
        var papers = await db.Newspapers.ToListAsync(ct);
        if (ids.Count != papers.Count || ids.Distinct().Count() != ids.Count || !papers.All(p => ids.Contains(p.Id)))
            throw new FieldValidationException("ids", "Donnez chaque journal une fois, sans en oublier.");

        var positions = ids.Select((paperId, i) => (paperId, i)).ToDictionary(x => x.paperId, x => x.i + 1);
        foreach (var paper in papers)
            paper.SortOrder = positions[paper.Id];
        await db.SaveChangesAsync(ct);
        return await ListAsync(ct);
    }

    /// <summary>Saisie directe : la valeur absolue remplace la quantité (dernier enregistrement gagnant, R-11).</summary>
    public Task<NewspaperSaleResponse> SetQuantityAsync(DateOnly date, Guid id, int quantity, CancellationToken ct) =>
        UpsertAsync(date, id, quantity, absolute: true, ct);

    /// <summary>
    /// <c>+1</c>/<c>−1</c> appliqué par la base dans une seule instruction : deux postes simultanés ne
    /// s'écrasent pas (R-09). La quantité reste entre 0 et 999 : un <c>−1</c> à 0 est ignoré.
    /// </summary>
    public Task<NewspaperSaleResponse> IncrementAsync(DateOnly date, Guid id, int delta, CancellationToken ct) =>
        UpsertAsync(date, id, delta, absolute: false, ct);

    private async Task<NewspaperSaleResponse> UpsertAsync(DateOnly date, Guid id, int value, bool absolute, CancellationToken ct)
    {
        var paper = await db.Newspapers.AsNoTracking().SingleOrDefaultAsync(n => n.Id == id, ct)
                    ?? throw new NotFoundException("Ce journal n'existe pas.");
        var sheet = await sheets.GetOrCreateAsync(date, ct);
        var now = time.GetUtcNow();
        var user = currentUser.UserId;
        var max = NewspaperSale.MaxQuantity;

        var quantity = absolute
            ? await db.Database.SqlQuery<int>($"""
                INSERT INTO "NewspaperSales" ("SheetId", "NewspaperId", "Quantity", "CreatedAt", "CreatedById")
                VALUES ({sheet.Id}, {id}, {value}, {now}, {user})
                ON CONFLICT ("SheetId", "NewspaperId") DO UPDATE
                SET "Quantity" = EXCLUDED."Quantity", "UpdatedAt" = {now}, "UpdatedById" = {user}
                RETURNING "Quantity" AS "Value"
                """).ToListAsync(ct)
            : await db.Database.SqlQuery<int>($"""
                INSERT INTO "NewspaperSales" ("SheetId", "NewspaperId", "Quantity", "CreatedAt", "CreatedById")
                VALUES ({sheet.Id}, {id}, GREATEST(0, LEAST({max}, {value})), {now}, {user})
                ON CONFLICT ("SheetId", "NewspaperId") DO UPDATE
                SET "Quantity" = GREATEST(0, LEAST({max}, "NewspaperSales"."Quantity" + {value})),
                    "UpdatedAt" = {now}, "UpdatedById" = {user}
                RETURNING "Quantity" AS "Value"
                """).ToListAsync(ct);
        return new NewspaperSaleResponse(paper.Id, paper.Name, paper.IsActive, quantity.Single());
    }

    private async Task<Newspaper> FindAsync(Guid id, CancellationToken ct) =>
        await db.Newspapers.SingleOrDefaultAsync(n => n.Id == id, ct)
        ?? throw new NotFoundException("Ce journal n'existe pas.");

    private async Task RenameAsync(Newspaper paper, string? name, CancellationToken ct)
    {
        var clean = CustomerNames.Clean(name);
        if (clean.Length == 0)
            throw new FieldValidationException("name", "Saisissez le nom du journal.");
        if (clean.Length > Newspaper.NameMaxLength)
            throw new FieldValidationException("name", "Le nom du journal fait au plus 100 caractères.");

        var normalized = CustomerNames.Normalize(clean);
        if (await db.Newspapers.AnyAsync(n => n.NormalizedName == normalized && n.Id != paper.Id, ct))
            throw DuplicateName();
        paper.Name = clean;
        paper.NormalizedName = normalized;
    }

    private async Task SaveNameAsync(CancellationToken ct)
    {
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException e) when (e.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            throw DuplicateName();
        }
    }

    private static ConflictException DuplicateName() => new("Un journal porte déjà ce nom.");

    private static NewspaperResponse From(Newspaper n) => new(n.Id, n.Name, n.IsActive, n.SortOrder);
}
