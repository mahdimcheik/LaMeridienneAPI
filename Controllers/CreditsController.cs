using LaMeridienneAPI.Dtos;
using LaMeridienneAPI.Models;
using LaMeridienneAPI.Services;
using Microsoft.AspNetCore.Mvc;

namespace LaMeridienneAPI.Controllers;

/// <summary>Crédits clients (E03), rattachées à la feuille d'une date.</summary>
[ApiController]
[Route("api")]
public sealed class CreditsController(EntryService entries) : ControllerBase
{
    [HttpPost("sheets/{date}/credits")]
    public async Task<ActionResult<EntryResponse>> Create(string date, EntryRequest request, CancellationToken ct) =>
        Created((string?)null, await entries.CreateAsync<CreditEntry>(SheetService.ParseDate(date), request, ct));

    [HttpPut("credits/{id:guid}")]
    public Task<EntryResponse> Update(Guid id, EntryRequest request, CancellationToken ct) =>
        entries.UpdateAsync<CreditEntry>(id, request, ct);

    [HttpDelete("credits/{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await entries.DeleteAsync<CreditEntry>(id, ct);
        return NoContent();
    }
}
