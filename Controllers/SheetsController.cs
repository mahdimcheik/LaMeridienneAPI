using LaMeridienneAPI.Dtos;
using LaMeridienneAPI.Services;
using Microsoft.AspNetCore.Mvc;

namespace LaMeridienneAPI.Controllers;

/// <summary>Feuille du jour (E02) et notes générales (E06).</summary>
[ApiController]
[Route("api/sheets")]
public sealed class SheetsController(SheetService sheets) : ControllerBase
{
    /// <summary>La feuille d'aujourd'hui (fuseau de la boutique), créée à l'ouverture (R-01).</summary>
    [HttpGet("today")]
    public async Task<SheetResponse> Today(CancellationToken ct)
    {
        var today = sheets.Today();
        await sheets.GetOrCreateAsync(today, ct);
        return await sheets.ReadAsync(today, ct);
    }

    /// <summary>Une date sans feuille rend une feuille vide, sans l'enregistrer.</summary>
    [HttpGet("{date}")]
    public Task<SheetResponse> Get(string date, CancellationToken ct) =>
        sheets.ReadAsync(SheetService.ParseDate(date), ct);

    [HttpPut("{date}/notes")]
    public Task<NotesResponse> SaveNotes(string date, NotesRequest request, CancellationToken ct) =>
        sheets.SaveNotesAsync(SheetService.ParseDate(date), request.Notes ?? "", request.Version!.Value, ct);
}
