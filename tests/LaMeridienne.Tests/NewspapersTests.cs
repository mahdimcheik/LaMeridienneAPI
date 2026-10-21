using System.Net;
using System.Net.Http.Json;
using LaMeridienneAPI.Dtos;
using LaMeridienneAPI.Models;
using Microsoft.Extensions.Time.Testing;

namespace LaMeridienne.Tests;

[Collection(DatabaseCollection.Name)]
public sealed class NewspapersTests(PostgresFixture db)
{
    private readonly FakeTimeProvider _time = new(DateTimeOffset.Parse("2026-10-21T08:00:00Z"));

    private Task<ApiFactory> FactoryAsync() =>
        ApiFactory.CreateAsync(db.ConnectionStringFor("newspapers_" + Guid.NewGuid().ToString("N")), time: _time);

    private static async Task<List<NewspaperResponse>> ListAsync(HttpClient client) =>
        (await client.GetFromJsonAsync<List<NewspaperResponse>>("/api/newspapers"))!;

    [Fact]
    public async Task R_10_Sud_Ouest_et_Le_Republicain_existent_au_premier_demarrage()
    {
        await using var factory = await FactoryAsync();
        var (client, _) = await factory.LoginAsync();

        var papers = await ListAsync(client);

        Assert.Equal([("Sud Ouest", true, 1), ("Le Républicain", true, 2)], papers.Select(p => (p.Name, p.IsActive, p.SortOrder)));
    }

    [Fact]
    public async Task US_E08_01_un_journal_ajoute_est_actif_et_se_place_en_dernier()
    {
        await using var factory = await FactoryAsync();
        var (client, _) = await factory.LoginAsync();

        var added = await client.AddNewspaperAsync("  L'Équipe ");

        Assert.Equal(("L'Équipe", true, 3), (added.Name, added.IsActive, added.SortOrder));
        Assert.Equal(["Sud Ouest", "Le Républicain", "L'Équipe"], (await client.SheetAsync("2026-10-21")).Newspapers.Select(n => n.Name));
    }

    [Theory]
    [InlineData("sud ouest")]
    [InlineData("LE REPUBLICAIN")]
    public async Task US_E08_01_un_nom_deja_pris_sans_casse_ni_accents_est_refuse(string name)
    {
        await using var factory = await FactoryAsync();
        var (client, _) = await factory.LoginAsync();
        var other = await client.AddNewspaperAsync("Libération");

        var created = await client.PostAsJsonAsync("/api/newspapers", new { name });
        var renamed = await client.PutAsJsonAsync($"/api/newspapers/{other.Id}", new { name });

        Assert.Equal(HttpStatusCode.Conflict, created.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, renamed.StatusCode);
    }

    [Fact]
    public async Task US_E08_01_un_nom_vide_est_refuse()
    {
        await using var factory = await FactoryAsync();
        var (client, _) = await factory.LoginAsync();

        var problem = await (await client.PostAsJsonAsync("/api/newspapers", new { name = "   " })).ValidationProblemAsync();

        Assert.Equal(["name"], problem.Errors.Keys);
    }

    [Fact]
    public async Task US_E08_01_renommer_s_applique_aux_feuilles_passees_et_garde_son_propre_nom()
    {
        await using var factory = await FactoryAsync();
        var (client, _) = await factory.LoginAsync();
        await client.SetQuantityAsync("2026-10-01", Newspaper.SudOuestId, 7);

        var sameName = await client.PutAsJsonAsync($"/api/newspapers/{Newspaper.SudOuestId}", new { name = "SUD OUEST" });
        var renamed = await client.PutAsJsonAsync($"/api/newspapers/{Newspaper.SudOuestId}", new { name = "Sud-Ouest" });

        Assert.Equal(HttpStatusCode.OK, sameName.StatusCode);
        Assert.Equal(HttpStatusCode.OK, renamed.StatusCode);
        var past = (await client.SheetAsync("2026-10-01")).Newspapers.Single(n => n.NewspaperId == Newspaper.SudOuestId);
        Assert.Equal(("Sud-Ouest", 7), (past.Name, past.Quantity));
    }

    [Fact]
    public async Task US_E08_01_l_ordre_choisi_est_celui_de_toutes_les_feuilles()
    {
        await using var factory = await FactoryAsync();
        var (client, _) = await factory.LoginAsync();
        var equipe = await client.AddNewspaperAsync("L'Équipe");

        var response = await client.PutAsJsonAsync("/api/newspapers/order",
            new { ids = new[] { equipe.Id, Newspaper.LeRepublicainId, Newspaper.SudOuestId } });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(["L'Équipe", "Le Républicain", "Sud Ouest"], (await client.SheetAsync("2026-09-01")).Newspapers.Select(n => n.Name));
    }

    [Fact]
    public async Task US_E08_01_un_ordre_incomplet_ou_en_double_est_refuse()
    {
        await using var factory = await FactoryAsync();
        var (client, _) = await factory.LoginAsync();

        var missing = await client.PutAsJsonAsync("/api/newspapers/order", new { ids = new[] { Newspaper.SudOuestId } });
        var twice = await client.PutAsJsonAsync("/api/newspapers/order", new { ids = new[] { Newspaper.SudOuestId, Newspaper.SudOuestId } });

        Assert.Equal(["ids"], (await missing.ValidationProblemAsync()).Errors.Keys);
        Assert.Equal(["ids"], (await twice.ValidationProblemAsync()).Errors.Keys);
        Assert.Equal([1, 2], (await ListAsync(client)).Select(p => p.SortOrder));
    }

    [Fact]
    public async Task US_E08_01_un_journal_desactive_peut_etre_reactive()
    {
        await using var factory = await FactoryAsync();
        var (client, _) = await factory.LoginAsync();

        await client.PutAsJsonAsync($"/api/newspapers/{Newspaper.LeRepublicainId}", new { name = "Le Républicain", isActive = false });
        var hidden = (await client.SheetAsync("2026-10-21")).Newspapers;
        await client.PutAsJsonAsync($"/api/newspapers/{Newspaper.LeRepublicainId}", new { name = "Le Républicain", isActive = true });
        var shown = (await client.SheetAsync("2026-10-21")).Newspapers;

        Assert.Equal(["Sud Ouest"], hidden.Select(n => n.Name));
        Assert.Equal(["Sud Ouest", "Le Républicain"], shown.Select(n => n.Name));
        Assert.Equal(2, (await ListAsync(client)).Count);
    }

    [Fact]
    public async Task US_E08_01_un_journal_inconnu_repond_404()
    {
        await using var factory = await FactoryAsync();
        var (client, _) = await factory.LoginAsync();

        var response = await client.PutAsJsonAsync($"/api/newspapers/{Guid.NewGuid()}", new { name = "X" });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
