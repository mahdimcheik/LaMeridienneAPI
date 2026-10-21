using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace LaMeridienneAPI.Infrastructure;

public static class ProblemDetailsSetup
{
    private static readonly Dictionary<int, string> Titles = new()
    {
        [StatusCodes.Status400BadRequest] = "Requête invalide",
        [StatusCodes.Status401Unauthorized] = "Authentification requise",
        [StatusCodes.Status403Forbidden] = "Accès refusé",
        [StatusCodes.Status404NotFound] = "Ressource introuvable",
        [StatusCodes.Status405MethodNotAllowed] = "Méthode non autorisée",
        [StatusCodes.Status409Conflict] = "Conflit",
        [StatusCodes.Status415UnsupportedMediaType] = "Type de contenu non pris en charge",
        [StatusCodes.Status423Locked] = "Compte verrouillé",
        [StatusCodes.Status429TooManyRequests] = "Trop de requêtes",
        [StatusCodes.Status500InternalServerError] = "Erreur interne",
        [StatusCodes.Status503ServiceUnavailable] = "Service indisponible",
    };

    public static IServiceCollection AddFrenchProblemDetails(this IServiceCollection services)
    {
        services.AddProblemDetails(o => o.CustomizeProblemDetails = ctx =>
        {
            var status = ctx.ProblemDetails.Status ?? ctx.HttpContext.Response.StatusCode;
            if (Titles.TryGetValue(status, out var title))
                ctx.ProblemDetails.Title = title;
            if (status == StatusCodes.Status400BadRequest && ctx.ProblemDetails is ValidationProblemDetails)
                ctx.ProblemDetails.Detail ??= "Un ou plusieurs champs sont invalides.";
        });
        services.AddExceptionHandler<ApiExceptionHandler>();
        services.AddExceptionHandler<UnhandledExceptionHandler>();
        services.Configure<ApiBehaviorOptions>(o =>
        {
            o.InvalidModelStateResponseFactory = ctx =>
            {
                var problem = new ValidationProblemDetails(ctx.ModelState)
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = Titles[StatusCodes.Status400BadRequest],
                    Detail = "Un ou plusieurs champs sont invalides.",
                };
                return new BadRequestObjectResult(problem) { ContentTypes = { "application/problem+json" } };
            };
        });
        return services;
    }
}

public sealed class UnhandledExceptionHandler(
    IProblemDetailsService problemDetails, IHostEnvironment env, ILogger<UnhandledExceptionHandler> logger)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext http, Exception exception, CancellationToken ct)
    {
        logger.LogError(exception, "Exception non gérée sur {Method} {Path}", http.Request.Method, http.Request.Path);

        http.Response.StatusCode = StatusCodes.Status500InternalServerError;
        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = http,
            Exception = exception,
            ProblemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Detail = env.IsDevelopment()
                    ? exception.Message
                    : "Une erreur inattendue est survenue. Réessayez ; si elle persiste, prévenez l'administrateur.",
            },
        });
    }
}
