using LaMeridienneAPI.Data;
using LaMeridienneAPI.Dtos;
using LaMeridienneAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace LaMeridienneAPI.Services;

/// <summary>Disposition des cases, une par utilisateur (R-07).</summary>
public sealed class LayoutService(AppDbContext db)
{
    public async Task<LayoutDto> GetAsync(Guid userId, CancellationToken ct) =>
        LayoutDto.From(await db.SheetLayouts.AsNoTracking().SingleOrDefaultAsync(l => l.UserId == userId, ct));

    public async Task<LayoutDto> SaveAsync(Guid userId, LayoutDto layout, CancellationToken ct)
    {
        var stored = await db.SheetLayouts.SingleOrDefaultAsync(l => l.UserId == userId, ct);
        if (stored is null)
            db.SheetLayouts.Add(stored = new SheetLayout { UserId = userId });
        stored.TopSplit = layout.TopSplit!.Value;
        stored.BottomSplit = layout.BottomSplit!.Value;
        stored.RowSplit = layout.RowSplit!.Value;
        await db.SaveChangesAsync(ct);
        return LayoutDto.From(stored);
    }
}
