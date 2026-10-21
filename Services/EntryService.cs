using LaMeridienneAPI.Data;
using LaMeridienneAPI.Dtos;
using LaMeridienneAPI.Infrastructure;
using LaMeridienneAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace LaMeridienneAPI.Services;

/// <summary>
/// Lignes de crédit (E03) et de remboursement (E04). Mêmes règles de saisie, tables et routes
/// séparées (R-04) ; le type de ligne est le paramètre <typeparamref name="T"/>.
/// </summary>
public sealed class EntryService(AppDbContext db, SheetService sheets, CustomerService customers)
{
    public async Task<EntryResponse> CreateAsync<T>(DateOnly date, EntryRequest request, CancellationToken ct)
        where T : CustomerEntry, new()
    {
        var customer = await customers.ResolveAsync(request.CustomerId, request.CustomerName, ct);
        var sheet = await sheets.GetOrCreateAsync(date, ct);
        var entry = new T { SheetId = sheet.Id, CustomerId = customer.Id };
        Apply(entry, request);
        db.Add(entry);
        await db.SaveChangesAsync(ct);
        return await ReadAsync<T>(entry.Id, ct);
    }

    /// <summary>Dernier enregistrement gagnant (R-11) ; une ligne supprimée entre-temps répond <c>404</c>.</summary>
    public async Task<EntryResponse> UpdateAsync<T>(Guid id, EntryRequest request, CancellationToken ct)
        where T : CustomerEntry
    {
        var entry = await FindAsync<T>(id, ct);
        entry.CustomerId = (await customers.ResolveAsync(request.CustomerId, request.CustomerName, ct)).Id;
        Apply(entry, request);
        await db.SaveChangesAsync(ct);
        return await ReadAsync<T>(id, ct);
    }

    /// <summary>Suppression logique (R-06), posée par l'intercepteur d'audit.</summary>
    public async Task DeleteAsync<T>(Guid id, CancellationToken ct) where T : CustomerEntry
    {
        db.Remove(await FindAsync<T>(id, ct));
        await db.SaveChangesAsync(ct);
    }

    private async Task<T> FindAsync<T>(Guid id, CancellationToken ct) where T : CustomerEntry =>
        await db.Set<T>().SingleOrDefaultAsync(e => e.Id == id, ct)
        ?? throw new NotFoundException("Cette ligne n'existe plus : rechargez la feuille.");

    private Task<EntryResponse> ReadAsync<T>(Guid id, CancellationToken ct) where T : CustomerEntry =>
        db.Set<T>().Where(e => e.Id == id).ToResponses(db).SingleAsync(ct);

    private static void Apply(CustomerEntry entry, EntryRequest request)
    {
        entry.Amount = request.Amount!.Value;
        var comment = request.Comment?.Trim();
        entry.Comment = string.IsNullOrEmpty(comment) ? null : comment;
    }
}
