using LaMeridienneAPI.Dtos;
using LaMeridienneAPI.Services;
using Microsoft.AspNetCore.Mvc;

namespace LaMeridienneAPI.Controllers;

/// <summary>Référentiel clients : autocomplétion (US-E03-04) et crédits d'un client (US-E03-05).</summary>
[ApiController]
[Route("api/customers")]
public sealed class CustomersController(CustomerService customers) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<CustomerResponse>> Search(string? search, int? limit, CancellationToken ct) =>
        customers.SearchAsync(search, limit, ct);

    [HttpGet("{id:guid}/credits")]
    public Task<CustomerCreditsResponse> Credits(Guid id, CancellationToken ct) =>
        customers.GetCreditsAsync(id, ct);
}
