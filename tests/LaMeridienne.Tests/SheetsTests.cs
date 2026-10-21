using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using LaMeridienneAPI.Data;
using LaMeridienneAPI.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;

namespace LaMeridienne.Tests;

[Collection(DatabaseCollection.Name)]
public sealed class SheetsTests(PostgresFixture db)
{
    private readonly FakeTimeProvider _time = new(DateTimeOffset.Parse("2026-10-21T08:00:00Z"));

    private Task<ApiFactory> FactoryAsync() =>
        ApiFactory.CreateAsync(db.ConnectionStringFor("sheets_" + Guid.NewGuid().ToString("N")), time: _time);

    private static async Task<int> SheetCountAsync(ApiFactory factory)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<AppDbContext>().DailySheets.CountAsync();
    }

    [Fact]
    public async Task R_01_la_feuille_du_jour_suit_le_fuseau_de_Paris_et_est_creee_a_l_ouverture()
    {
        _time.SetUtcNow(DateTimeOffset.Parse("2026-10-21T22:30:00Z")); // 00:30 le 22 à Paris
        await using var factory = await FactoryAsync();
        var (client, _) = await factory.LoginAsync();

        var sheet = await client.GetFromJsonAsync<SheetResponse>("/api/sheets/today");

        Assert.Equal(new DateOnly(2026, 10, 22), sheet!.Date);
        Assert.True(sheet.Exists);
        Assert.Equal(("", 0), (sheet.Notes, sheet.NotesVersion));
        Assert.Empty(sheet.Credits);
        Assert.Equal(1, await SheetCountAsync(factory));
    }

    [Fact]
    public async Task US_E02_01_deux_ouvertures_simultanees_ne_creent_qu_une_feuille()
    {
        await using var factory = await FactoryAsync();
        var (client, _) = await factory.LoginAsync();

        var responses = await Task.WhenAll(Enumerable.Range(0, 10).Select(_ => client.GetAsync("/api/sheets/today")));

        Assert.All(responses, r => Assert.Equal(HttpStatusCode.OK, r.StatusCode));
        Assert.Equal(1, await SheetCountAsync(factory));
    }

    [Fact]
    public async Task US_E02_02_une_date_sans_feuille_s_affiche_vide_sans_etre_enregistree()
    {
        await using var factory = await FactoryAsync();
        var (client, _) = await factory.LoginAsync();

        var sheet = await client.SheetAsync("2026-10-01");

        Assert.Equal(new DateOnly(2026, 10, 1), sheet.Date);
        Assert.False(sheet.Exists);
        Assert.Equal(0, await SheetCountAsync(factory));
    }

    [Theory]
    [InlineData("2026-13-01")]
    [InlineData("21-10-2026")]
    [InlineData("demain")]
    public async Task US_E02_02_une_date_mal_formee_repond_400(string date)
    {
        await using var factory = await FactoryAsync();
        var (client, _) = await factory.LoginAsync();

        var problem = await (await client.GetAsync($"/api/sheets/{date}")).ValidationProblemAsync();

        Assert.Equal(["date"], problem.Errors.Keys);
    }

    [Fact]
    public async Task US_E01_01_la_feuille_exige_un_jeton()
    {
        await using var factory = await FactoryAsync();

        var response = await factory.CreateClient().GetAsync("/api/sheets/today");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task US_E06_01_les_notes_sont_enregistrees_telles_quelles_et_versionnees()
    {
        await using var factory = await FactoryAsync();
        var (client, _) = await factory.LoginAsync();
        const string notes = "Livraison Sud Ouest en retard\n<b>pas du HTML</b>\n\nRappeler M. Martin";

        var saved = await client.PutAsJsonAsync("/api/sheets/2026-10-20/notes", new { notes, version = 0 });

        Assert.Equal(HttpStatusCode.OK, saved.StatusCode);
        Assert.Equal(new NotesResponse(notes, 1), await saved.Content.ReadFromJsonAsync<NotesResponse>());
        var sheet = await client.SheetAsync("2026-10-20");
        Assert.Equal((true, notes, 1), (sheet.Exists, sheet.Notes, sheet.NotesVersion));
    }

    [Fact]
    public async Task R_11_des_notes_fondees_sur_une_version_perimee_repondent_409_avec_le_texte_serveur()
    {
        await using var factory = await FactoryAsync();
        var (marie, _) = await factory.LoginAsync(0);
        var (paul, _) = await factory.LoginAsync(1);
        await marie.PutAsJsonAsync("/api/sheets/2026-10-21/notes", new { notes = "version de Marie", version = 0 });

        var stale = await paul.PutAsJsonAsync("/api/sheets/2026-10-21/notes", new { notes = "version de Paul", version = 0 });

        Assert.Equal(HttpStatusCode.Conflict, stale.StatusCode);
        var problem = await stale.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Equal("Conflit", problem!.Title);
        Assert.Equal("version de Marie", ((JsonElement)problem.Extensions["notes"]!).GetString());
        Assert.Equal(1, ((JsonElement)problem.Extensions["notesVersion"]!).GetInt32());
        Assert.Equal("version de Marie", (await paul.SheetAsync("2026-10-21")).Notes);
    }

    [Fact]
    public async Task R_11_deux_enregistrements_simultanes_sur_la_meme_version_un_seul_l_emporte()
    {
        await using var factory = await FactoryAsync();
        var (client, _) = await factory.LoginAsync();
        await client.PutAsJsonAsync("/api/sheets/2026-10-21/notes", new { notes = "départ", version = 0 });

        var responses = await Task.WhenAll(Enumerable.Range(0, 5).Select(i =>
            client.PutAsJsonAsync("/api/sheets/2026-10-21/notes", new { notes = $"poste {i}", version = 1 })));

        Assert.Single(responses, r => r.StatusCode == HttpStatusCode.OK);
        Assert.All(responses, r => Assert.Contains(r.StatusCode, new[] { HttpStatusCode.OK, HttpStatusCode.Conflict }));
        Assert.Equal(2, (await client.SheetAsync("2026-10-21")).NotesVersion);
    }

    [Fact]
    public async Task US_E06_01_des_notes_de_plus_de_10_000_caracteres_sont_refusees()
    {
        await using var factory = await FactoryAsync();
        var (client, _) = await factory.LoginAsync();

        var response = await client.PutAsJsonAsync("/api/sheets/2026-10-21/notes", new { notes = new string('a', 10_001), version = 0 });

        Assert.Equal(["notes"], (await response.ValidationProblemAsync()).Errors.Keys);
        Assert.Equal(0, await SheetCountAsync(factory));
    }
}
