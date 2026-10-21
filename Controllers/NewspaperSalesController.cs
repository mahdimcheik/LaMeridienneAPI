using LaMeridienneAPI.Dtos;
using LaMeridienneAPI.Services;
using Microsoft.AspNetCore.Mvc;

namespace LaMeridienneAPI.Controllers;

/// <summary>Quantités vendues par journal sur la feuille d'une date (E05).</summary>
[ApiController]
[Route("api/sheets/{date}/newspapers/{newspaperId:guid}")]
public sealed class NewspaperSalesController(NewspaperService newspapers) : ControllerBase
{
    /// <summary>Saisie directe de la quantité.</summary>
    [HttpPut]
    public Task<NewspaperSaleResponse> Set(string date, Guid newspaperId, SaleQuantityRequest request, CancellationToken ct) =>
        newspapers.SetQuantityAsync(SheetService.ParseDate(date), newspaperId, request.Quantity!.Value, ct);

    /// <summary>Boutons <c>+1</c>/<c>−1</c> : incrément atomique (R-09).</summary>
    [HttpPost("increment")]
    public Task<NewspaperSaleResponse> Increment(string date, Guid newspaperId, SaleIncrementRequest request, CancellationToken ct) =>
        newspapers.IncrementAsync(SheetService.ParseDate(date), newspaperId, request.Delta!.Value, ct);
}
