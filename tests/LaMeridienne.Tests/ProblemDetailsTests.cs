using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace LaMeridienne.Tests;

public sealed class ProblemDetailsTests
{
    private sealed class ThrowingApiFactory() : ApiFactory("Host=127.0.0.1;Port=1;Database=absente;Username=x;Password=x")
    {

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);
            builder.ConfigureServices(s => s.AddTransient<Microsoft.AspNetCore.Hosting.IStartupFilter, BoomFilter>());
        }
    }

    private sealed class BoomFilter : Microsoft.AspNetCore.Hosting.IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
        {
            next(app);
            app.Map("/test/boom", b => b.Run(_ => throw new InvalidOperationException("secret interne")));
        };
    }

    [Fact]
    public async Task Erreur_inattendue_rend_un_problem_details_en_francais_sans_fuite()
    {
        await using var factory = new ThrowingApiFactory { WithSeeder = false };

        var response = await factory.CreateClient().GetAsync("/test/boom");

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Equal("Erreur interne", problem!.Title);
        Assert.DoesNotContain("secret interne", problem.Detail);
    }

    [Fact]
    public async Task Route_inconnue_rend_un_404_en_francais()
    {
        await using var factory = new ThrowingApiFactory { WithSeeder = false };

        var response = await factory.CreateClient().GetAsync("/api/inexistante");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Equal("Ressource introuvable", problem!.Title);
    }
}
