using LaMeridienneAPI.Dtos;
using LaMeridienneAPI.Models;
using LaMeridienneAPI.Services;
using Microsoft.AspNetCore.Mvc;

namespace LaMeridienneAPI.Controllers;

/// <summary>Remboursements (E04), rattachées à la feuille d'une date.</summary>
[ApiController]
[Route("api")]
public sealed class RefundsController(EntryService entries) : ControllerBase
{
    [HttpPost("sheets/{date}/refunds")]
    public async Task<ActionResult<EntryResponse>> Create(string date, EntryRequest request, CancellationToken ct) =>
        Created((string?)null, await entries.CreateAsync<RefundEntry>(SheetService.ParseDate(date), request, ct));

    [HttpPut("refunds/{id:guid}")]
    public Task<EntryResponse> Update(Guid id, EntryRequest request, CancellationToken ct) =>
        entries.UpdateAsync<RefundEntry>(id, request, ct);

    [HttpDelete("refunds/{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await entries.DeleteAsync<RefundEntry>(id, ct);
        return NoContent();
    }
}
