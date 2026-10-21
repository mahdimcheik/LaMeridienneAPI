using LaMeridienneAPI.Dtos;
using LaMeridienneAPI.Services;
using Microsoft.AspNetCore.Mvc;

namespace LaMeridienneAPI.Controllers;

/// <summary>Liste des journaux (US-E08-01) : jamais supprimés, seulement désactivés (R-10).</summary>
[ApiController]
[Route("api/newspapers")]
public sealed class NewspapersController(NewspaperService newspapers) : ControllerBase
{
    /// <summary>Tous les journaux, désactivés compris, dans l'ordre de la case Journaux.</summary>
    [HttpGet]
    public Task<List<NewspaperResponse>> List(CancellationToken ct) => newspapers.ListAsync(ct);

    [HttpPost]
    public async Task<ActionResult<NewspaperResponse>> Create(NewspaperRequest request, CancellationToken ct) =>
        Created((string?)null, await newspapers.CreateAsync(request.Name, ct));

    [HttpPut("{id:guid}")]
    public Task<NewspaperResponse> Update(Guid id, NewspaperRequest request, CancellationToken ct) =>
        newspapers.UpdateAsync(id, request, ct);

    [HttpPut("order")]
    public Task<List<NewspaperResponse>> Reorder(NewspaperOrderRequest request, CancellationToken ct) =>
        newspapers.ReorderAsync(request.Ids!, ct);
}
