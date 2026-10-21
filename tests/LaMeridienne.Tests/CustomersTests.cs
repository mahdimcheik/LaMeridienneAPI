using System.Net;
using System.Net.Http.Json;
using LaMeridienneAPI.Dtos;
using LaMeridienneAPI.Services;
using Microsoft.Extensions.Time.Testing;

namespace LaMeridienne.Tests;

[Collection(DatabaseCollection.Name)]
public sealed class CustomersTests(PostgresFixture db)
{
    private readonly FakeTimeProvider _time = new(DateTimeOffset.Parse("2026-10-21T08:00:00Z"));

    private Task<ApiFactory> FactoryAsync() =>
        ApiFactory.CreateAsync(db.ConnectionStringFor("customers_" + Guid.NewGuid().ToString("N")), time: _time);

    [Theory]
    [InlineData("  Jean   Dupont ", "Jean Dupont", "jean dupont")]
    [InlineData("ÉLODIE Lefèvre", "ÉLODIE Lefèvre", "elodie lefevre")]
    [InlineData("Cœur de Lion", "Cœur de Lion", "coeur de lion")]
    public void R_08_les_noms_sont_nettoyes_et_compares_sans_casse_ni_accents(string input, string clean, string normalized)
    {
        Assert.Equal(clean, CustomerNames.Clean(input));
        Assert.Equal(normalized, CustomerNames.Normalize(input));
    }

    [Fact]
    public async Task US_E03_04_la_recherche_trouve_les_noms_qui_contiennent_le_terme_sans_casse_ni_accents()
    {
        await using var factory = await FactoryAsync();
        var (client, _) = await factory.LoginAsync();
        foreach (var name in new[] { "Hélène Martin", "Martine Roux", "Dupont", "Lemartinet", "100% Bio" })
            await client.AddEntryAsync("credits", "2026-10-21", new { customerName = name, amount = 1 });

        var found = await client.GetFromJsonAsync<List<CustomerResponse>>("/api/customers?search=MARTIN");
        var percent = await client.GetFromJsonAsync<List<CustomerResponse>>("/api/customers?search=0%25");
        var accent = await client.GetFromJsonAsync<List<CustomerResponse>>("/api/customers?search=hele");

        Assert.Equal(["Martine Roux", "Hélène Martin", "Lemartinet"], found!.Select(c => c.Name));
        Assert.Equal(["100% Bio"], percent!.Select(c => c.Name));
        Assert.Equal(["Hélène Martin"], accent!.Select(c => c.Name));
    }

    [Fact]
    public async Task US_E03_04_au_plus_10_propositions()
    {
        await using var factory = await FactoryAsync();
        var (client, _) = await factory.LoginAsync();
        for (var i = 0; i < 12; i++)
            await client.AddEntryAsync("credits", "2026-10-21", new { customerName = $"Client {i:00}", amount = 1 });

        var found = await client.GetFromJsonAsync<List<CustomerResponse>>("/api/customers?search=client");
        var limited = await client.GetFromJsonAsync<List<CustomerResponse>>("/api/customers?search=client&limit=3");

        Assert.Equal(10, found!.Count);
        Assert.Equal(3, limited!.Count);
    }

    [Theory]
    [InlineData("?search=a", "search")]
    [InlineData("?search=%20%20b%20", "search")]
    [InlineData("", "search")]
    [InlineData("?search=ab&limit=11", "limit")]
    public async Task US_E03_04_une_recherche_de_moins_de_2_caracteres_est_refusee(string query, string field)
    {
        await using var factory = await FactoryAsync();
        var (client, _) = await factory.LoginAsync();

        var problem = await (await client.GetAsync("/api/customers" + query)).ValidationProblemAsync();

        Assert.Equal([field], problem.Errors.Keys);
    }

    [Fact]
    public async Task US_E03_05_les_credits_d_un_client_toutes_dates_du_plus_recent_au_plus_ancien()
    {
        await using var factory = await FactoryAsync();
        var (client, _) = await factory.LoginAsync();
        var first = await client.AddEntryAsync("credits", "2026-10-01", new { customerName = "Dupont", amount = 1 });
        _time.Advance(TimeSpan.FromMinutes(1));
        await client.AddEntryAsync("credits", "2026-10-21", new { customerName = "Dupont", amount = 2 });
        _time.Advance(TimeSpan.FromMinutes(1));
        await client.AddEntryAsync("credits", "2026-10-21", new { customerName = "Dupont", amount = 3 });
        await client.AddEntryAsync("credits", "2026-10-15", new { customerName = "Dupont", amount = 4 });
        await client.AddEntryAsync("refunds", "2026-10-21", new { customerName = "Dupont", amount = 99 });
        await client.AddEntryAsync("credits", "2026-10-21", new { customerName = "Martin", amount = 98 });

        var result = await client.GetFromJsonAsync<CustomerCreditsResponse>($"/api/customers/{first.Customer.Id}/credits");

        Assert.Equal("Dupont", result!.Customer.Name);
        Assert.Equal([3m, 2m, 4m, 1m], result.Credits.Select(c => c.Amount));
        Assert.Equal(new DateOnly(2026, 10, 21), result.Credits[0].Date);
    }

    [Fact]
    public async Task US_E03_05_un_client_inconnu_repond_404()
    {
        await using var factory = await FactoryAsync();
        var (client, _) = await factory.LoginAsync();

        var response = await client.GetAsync($"/api/customers/{Guid.NewGuid()}/credits");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }
}
