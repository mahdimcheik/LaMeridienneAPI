using System.Net;
using System.Net.Http.Json;
using LaMeridienneAPI.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LaMeridienne.Tests;

/// <summary>Mise en service : migration au démarrage et API derrière le reverse proxy.</summary>
[Collection(DatabaseCollection.Name)]
public sealed class DeploymentTests(PostgresFixture db)
{
    private string NewDatabase(string prefix) => db.ConnectionStringFor(prefix + Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task Le_demarrage_cree_la_base_et_applique_toutes_les_migrations()
    {
        await using var factory = new ApiFactory(NewDatabase("migrate_"));

        await using var scope = factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Empty(await context.Database.GetPendingMigrationsAsync());
        Assert.Equal(context.Database.GetMigrations(), await context.Database.GetAppliedMigrationsAsync());
    }

    [Fact]
    public async Task Derriere_le_proxy_la_limite_de_connexion_est_comptee_par_adresse_client()
    {
        await using var factory = await ApiFactory.CreateAsync(NewDatabase("proxy_"),
            f => f.Settings["FORWARDEDHEADERS_ENABLED"] = "true");
        var client = factory.CreateClient();

        Task<HttpResponseMessage> LoginFrom(string ip)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/login")
            {
                Content = JsonContent.Create(new { userName = "personne", password = "x" }),
            };
            request.Headers.Add("X-Forwarded-For", ip);
            request.Headers.Add("X-Forwarded-Proto", "https");
            return client.SendAsync(request);
        }

        for (var i = 0; i < 10; i++)
            Assert.Equal(HttpStatusCode.Unauthorized, (await LoginFrom("203.0.113.1")).StatusCode);

        Assert.Equal(HttpStatusCode.TooManyRequests, (await LoginFrom("203.0.113.1")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await LoginFrom("203.0.113.2")).StatusCode);
    }
}
