using LaMeridienneAPI.Dtos;
using LaMeridienneAPI.Services;
using Microsoft.AspNetCore.Mvc;

namespace LaMeridienneAPI.Controllers;

/// <summary>
/// Référentiel clients : autocomplétion (US-E03-04), crédits d'un client (US-E03-05), liste,
/// renommage et fusion (US-E08-02).
/// </summary>
[ApiController]
[Route("api/customers")]
public sealed class CustomersController(CustomerService customers) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<CustomerResponse>> Search(string? search, int? limit, CancellationToken ct) =>
        customers.SearchAsync(search, limit, ct);

    [HttpGet("list")]
    public Task<CustomerPageResponse> List(string? search, int? page, int? pageSize, CancellationToken ct) =>
        customers.ListAsync(search, page, pageSize, ct);

    [HttpGet("{id:guid}/credits")]
    public Task<CustomerCreditsResponse> Credits(Guid id, CancellationToken ct) =>
        customers.GetCreditsAsync(id, ct);

    [HttpPut("{id:guid}")]
    public Task<CustomerResponse> Update(Guid id, CustomerUpdateRequest request, CancellationToken ct) =>
        customers.UpdateAsync(id, request, ct);

    [HttpPost("{id:guid}/merge")]
    public Task<CustomerResponse> Merge(Guid id, CustomerMergeRequest request, CancellationToken ct) =>
        customers.MergeAsync(id, request.SourceId!.Value, ct);
}
