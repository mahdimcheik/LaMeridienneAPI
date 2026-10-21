using LaMeridienneAPI.Dtos;
using LaMeridienneAPI.Services;
using Microsoft.AspNetCore.Mvc;

namespace LaMeridienneAPI.Controllers;

/// <summary>Agenda des pneus (E07).</summary>
[ApiController]
[Route("api/appointments")]
public sealed class AppointmentsController(AppointmentService appointments) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<AppointmentResponse>> List(DateTimeOffset? from, DateTimeOffset? to, CancellationToken ct) =>
        appointments.ListAsync(from, to, ct);

    [HttpPost]
    public async Task<ActionResult<AppointmentSaveResponse>> Create(AppointmentRequest request, CancellationToken ct) =>
        Created((string?)null, await appointments.CreateAsync(request, ct));

    [HttpPut("{id:guid}")]
    public Task<AppointmentSaveResponse> Update(Guid id, AppointmentRequest request, CancellationToken ct) =>
        appointments.UpdateAsync(id, request, ct);

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await appointments.DeleteAsync(id, ct);
        return NoContent();
    }
}
