using System.Globalization;
using LaMeridienneAPI.Data;
using LaMeridienneAPI.Dtos;
using LaMeridienneAPI.Infrastructure;
using LaMeridienneAPI.Models;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace LaMeridienneAPI.Services;

/// <summary>Feuilles par date (R-01, ADR-0004) et notes versionnées (R-11).</summary>
public sealed class SheetService(AppDbContext db, ShopClock clock)
{
    /// <summary>Date d'une route, au format <c>AAAA-MM-JJ</c> ; sinon <c>400</c>.</summary>
    public static DateOnly ParseDate(string value) =>
        DateOnly.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)
            ? date
            : throw new FieldValidationException("date", "Date invalide : format attendu AAAA-MM-JJ.");

    public DateOnly Today() => clock.Today();

    /// <summary>
    /// La feuille de la date, créée si besoin. Deux créations simultanées : la seconde bute sur
    /// l'index unique et relit celle de la première.
    /// </summary>
    public async Task<DailySheet> GetOrCreateAsync(DateOnly date, CancellationToken ct)
    {
        var sheet = await db.DailySheets.SingleOrDefaultAsync(s => s.Date == date, ct);
        if (sheet is not null) return sheet;

        sheet = new DailySheet { Date = date };
        db.DailySheets.Add(sheet);
        try
        {
            await db.SaveChangesAsync(ct);
            return sheet;
        }
        catch (DbUpdateException e) when (e.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            db.Entry(sheet).State = EntityState.Detached;
            return await db.DailySheets.SingleAsync(s => s.Date == date, ct);
        }
    }

    /// <summary>La feuille d'une date ; une date sans feuille rend une feuille vide, non enregistrée.</summary>
    public async Task<SheetResponse> ReadAsync(DateOnly date, CancellationToken ct)
    {
        var sheet = await db.DailySheets.AsNoTracking().SingleOrDefaultAsync(s => s.Date == date, ct);
        if (sheet is null)
            return new SheetResponse(date, false, "", 0, [], []);

        var credits = await db.CreditEntries.Where(e => e.SheetId == sheet.Id)
            .OrderBy(e => e.CreatedAt).ToResponses(db).ToListAsync(ct);
        var refunds = await db.RefundEntries.Where(e => e.SheetId == sheet.Id)
            .OrderBy(e => e.CreatedAt).ToResponses(db).ToListAsync(ct);
        return new SheetResponse(date, true, sheet.Notes, sheet.NotesVersion, credits, refunds);
    }

    /// <summary>Enregistre les notes si <paramref name="version"/> est la version courante ; sinon <c>409</c>.</summary>
    public async Task<NotesResponse> SaveNotesAsync(DateOnly date, string notes, int version, CancellationToken ct)
    {
        var sheet = await GetOrCreateAsync(date, ct);
        if (sheet.NotesVersion != version)
            throw Stale(sheet);

        sheet.Notes = notes;
        sheet.NotesVersion++;
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            await db.Entry(sheet).ReloadAsync(ct);
            throw Stale(sheet);
        }
        return new NotesResponse(sheet.Notes, sheet.NotesVersion);
    }

    private static ConflictException Stale(DailySheet sheet) => new(
        "Les notes ont été modifiées depuis un autre poste.",
        new Dictionary<string, object?> { ["notes"] = sheet.Notes, ["notesVersion"] = sheet.NotesVersion });
}
