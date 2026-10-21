using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace LaMeridienneAPI.Infrastructure;

/// <summary>Ressource inconnue ou supprimée : <c>404</c>.</summary>
public sealed class NotFoundException(string detail) : Exception(detail);

/// <summary>Entrée refusée par une règle de service : <c>400</c> avec l'erreur sous la clé JSON du champ.</summary>
public sealed class FieldValidationException(string field, string message) : Exception(message)
{
    public string Field { get; } = field;
}

/// <summary>Conflit de version ou de nom : <c>409</c>, avec l'état serveur en extensions.</summary>
public sealed class ConflictException(string detail, IDictionary<string, object?>? extensions = null) : Exception(detail)
{
    public IDictionary<string, object?> Extensions { get; } = extensions ?? new Dictionary<string, object?>();
}

public sealed class ApiExceptionHandler(IProblemDetailsService problemDetails) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext http, Exception exception, CancellationToken ct)
    {
        ProblemDetails? problem = exception switch
        {
            NotFoundException e => new ProblemDetails { Status = StatusCodes.Status404NotFound, Detail = e.Message },
            FieldValidationException e => new ValidationProblemDetails(
                new Dictionary<string, string[]> { [e.Field] = [e.Message] }) { Status = StatusCodes.Status400BadRequest },
            ConflictException e => new ProblemDetails { Status = StatusCodes.Status409Conflict, Detail = e.Message },
            _ => null,
        };
        if (problem is null) return false;
        if (exception is ConflictException conflict)
            foreach (var (key, value) in conflict.Extensions)
                problem.Extensions[key] = value;

        http.Response.StatusCode = problem.Status!.Value;
        return await problemDetails.TryWriteAsync(new ProblemDetailsContext { HttpContext = http, ProblemDetails = problem });
    }
}
