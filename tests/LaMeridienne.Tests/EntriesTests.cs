using System.Net;
using System.Net.Http.Json;
using LaMeridienneAPI.Data;
using LaMeridienneAPI.Dtos;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;

namespace LaMeridienne.Tests;

/// <summary>Crédits (E03) et remboursements (E04) : mêmes règles, vérifiées pour les deux.</summary>
[Collection(DatabaseCollection.Name)]
public sealed class EntriesTests(PostgresFixture db)
{
    private const string Day = "2026-10-21";
    private readonly FakeTimeProvider _time = new(DateTimeOffset.Parse("2026-10-21T08:00:00Z"));

    private Task<ApiFactory> FactoryAsync() =>
        ApiFactory.CreateAsync(db.ConnectionStringFor("entries_" + Guid.NewGuid().ToString("N")), time: _time);

    private static IReadOnlyList<EntryResponse> Lines(SheetResponse sheet, string kind) =>
        kind == "credits" ? sheet.Credits : sheet.Refunds;

    private static string Other(string kind) => kind == "credits" ? "refunds" : "credits";

    [Theory]
    [InlineData("credits")]
    [InlineData("refunds")]
    public async Task US_E03_01_US_E04_01_une_ligne_saisie_par_nom_apparait_dans_sa_seule_case_avec_son_auteur(string kind)
    {
        await using var factory = await FactoryAsync();
        var (client, _) = await factory.LoginAsync();

        var entry = await client.AddEntryAsync(kind, Day,
            new { customerName = "  Dupont  ", amount = 12.5m, comment = " pain + journal " });

        Assert.Equal(("Dupont", 12.5m, "pain + journal"), (entry.Customer.Name, entry.Amount, entry.Comment));
        Assert.Equal(("Marie", _time.GetUtcNow()), (entry.CreatedBy, entry.CreatedAt));
        Assert.Null(entry.UpdatedBy);
        var sheet = await client.SheetAsync(Day);
        Assert.Equal([entry], Lines(sheet, kind));
        Assert.Empty(Lines(sheet, Other(kind)));
    }

    [Fact]
    public async Task Les_instants_sont_rendus_en_UTC_avec_Z()
    {
        await using var factory = await FactoryAsync();
        var (client, _) = await factory.LoginAsync();

        var response = await client.PostEntryAsync("credits", Day, new { customerName = "Dupont", amount = 1 });

        Assert.Contains("\"createdAt\":\"2026-10-21T08:00:00Z\"", await response.Content.ReadAsStringAsync());
    }

    [Theory]
    [InlineData("credits")]
    [InlineData("refunds")]
    public async Task R_08_un_nom_connu_sans_casse_ni_accents_reutilise_la_fiche(string kind)
    {
        await using var factory = await FactoryAsync();
        var (client, _) = await factory.LoginAsync();

        var first = await client.AddEntryAsync(kind, Day, new { customerName = "Élodie   Lefèvre", amount = 3 });
        var second = await client.AddEntryAsync(kind, Day, new { customerName = "elodie lefevre", amount = 4 });
        var byId = await client.AddEntryAsync(kind, "2026-10-22", new { customerId = first.Customer.Id, amount = 5 });

        Assert.Equal(new CustomerRef(first.Customer.Id, "Élodie Lefèvre"), second.Customer);
        Assert.Equal(first.Customer, byId.Customer);
    }

    [Theory]
    [InlineData("credits", 0)]
    [InlineData("credits", -1)]
    [InlineData("credits", 10000.01)]
    [InlineData("credits", 1.234)]
    [InlineData("refunds", 0)]
    [InlineData("refunds", 10000.01)]
    public async Task R_05_un_montant_invalide_est_refuse_et_designe_le_champ(string kind, double amount)
    {
        await using var factory = await FactoryAsync();
        var (client, _) = await factory.LoginAsync();

        var problem = await (await client.PostEntryAsync(kind, Day, new { customerName = "Dupont", amount = (decimal)amount }))
            .ValidationProblemAsync();

        Assert.Equal(["amount"], problem.Errors.Keys);
    }

    [Theory]
    [InlineData("credits")]
    [InlineData("refunds")]
    public async Task R_05_le_plafond_de_10_000_euros_est_accepte(string kind)
    {
        await using var factory = await FactoryAsync();
        var (client, _) = await factory.LoginAsync();

        Assert.Equal(10_000m, (await client.AddEntryAsync(kind, Day, new { customerName = "Dupont", amount = 10_000 })).Amount);
    }

    [Theory]
    [InlineData("credits", "{\"customerName\":\"Dupont\"}", "amount")]
    [InlineData("credits", "{\"amount\":5}", "customerName")]
    [InlineData("credits", "{\"customerName\":\"   \",\"amount\":5}", "customerName")]
    [InlineData("refunds", "{\"customerId\":\"0199f000-0000-7000-8000-000000000000\",\"amount\":5}", "customerId")]
    [InlineData("refunds", "{\"customerId\":\"0199f000-0000-7000-8000-000000000000\",\"customerName\":\"Dupont\",\"amount\":5}", "customerName")]
    public async Task US_E03_01_un_client_ou_un_montant_manquant_est_refuse(string kind, string body, string field)
    {
        await using var factory = await FactoryAsync();
        var (client, _) = await factory.LoginAsync();

        var response = await client.PostAsync($"/api/sheets/{Day}/{kind}",
            new StringContent(body, System.Text.Encoding.UTF8, "application/json"));

        Assert.Equal([field], (await response.ValidationProblemAsync()).Errors.Keys);
    }

    [Theory]
    [InlineData("credits")]
    [InlineData("refunds")]
    public async Task US_E03_02_US_E09_03_une_correction_garde_l_auteur_et_ignore_l_auteur_envoye(string kind)
    {
        await using var factory = await FactoryAsync();
        var (marie, _) = await factory.LoginAsync(0);
        var (paul, _) = await factory.LoginAsync(1);
        var entry = await marie.AddEntryAsync(kind, Day,
            new { customerName = "Dupont", amount = 12, createdBy = "Pirate", createdById = Guid.NewGuid() });
        _time.Advance(TimeSpan.FromMinutes(10));

        var response = await paul.PutAsJsonAsync($"/api/{kind}/{entry.Id}",
            new { customerName = "Martin", amount = 15.4, comment = "corrigé", updatedBy = "Pirate" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = (await response.Content.ReadFromJsonAsync<EntryResponse>())!;
        Assert.Equal(("Martin", 15.4m, "corrigé"), (updated.Customer.Name, updated.Amount, updated.Comment));
        Assert.Equal(("Marie", entry.CreatedAt), (updated.CreatedBy, updated.CreatedAt));
        Assert.Equal(("Paul", _time.GetUtcNow()), (updated.UpdatedBy, updated.UpdatedAt));
        Assert.Equal([updated], Lines(await marie.SheetAsync(Day), kind));
    }

    [Theory]
    [InlineData("credits")]
    [InlineData("refunds")]
    public async Task US_E03_03_la_suppression_est_logique_et_une_ligne_supprimee_repond_404(string kind)
    {
        await using var factory = await FactoryAsync();
        var (client, _) = await factory.LoginAsync();
        var entry = await client.AddEntryAsync(kind, Day, new { customerName = "Dupont", amount = 12 });

        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/{kind}/{entry.Id}")).StatusCode);

        Assert.Empty(Lines(await client.SheetAsync(Day), kind));
        Assert.Equal(HttpStatusCode.NotFound, (await client.DeleteAsync($"/api/{kind}/{entry.Id}")).StatusCode);
        var update = await client.PutAsJsonAsync($"/api/{kind}/{entry.Id}", new { customerName = "Dupont", amount = 1 });
        Assert.Equal(HttpStatusCode.NotFound, update.StatusCode);
        Assert.Equal("application/problem+json", update.Content.Headers.ContentType?.MediaType);

        await using var scope = factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var deletedAt = kind == "credits"
            ? (await context.CreditEntries.IgnoreQueryFilters().SingleAsync(e => e.Id == entry.Id)).DeletedAt
            : (await context.RefundEntries.IgnoreQueryFilters().SingleAsync(e => e.Id == entry.Id)).DeletedAt;
        Assert.Equal(_time.GetUtcNow(), deletedAt);
    }

    [Fact]
    public async Task R_04_un_credit_ne_se_modifie_pas_par_la_route_des_remboursements()
    {
        await using var factory = await FactoryAsync();
        var (client, _) = await factory.LoginAsync();
        var credit = await client.AddEntryAsync("credits", Day, new { customerName = "Dupont", amount = 12 });

        Assert.Equal(HttpStatusCode.NotFound, (await client.DeleteAsync($"/api/refunds/{credit.Id}")).StatusCode);
    }

    [Fact]
    public async Task R_08_des_saisies_simultanees_d_un_nouveau_nom_ne_creent_qu_un_client()
    {
        await using var factory = await FactoryAsync();
        var (client, _) = await factory.LoginAsync();

        var entries = await Task.WhenAll(Enumerable.Range(0, 6).Select(i =>
            client.AddEntryAsync(i % 2 == 0 ? "credits" : "refunds", Day, new { customerName = "Nouveau Client", amount = 1 + i })));

        Assert.Single(entries.Select(e => e.Customer.Id).Distinct());
        Assert.Equal(6, (await client.SheetAsync(Day)) is var s ? s.Credits.Count + s.Refunds.Count : 0);
    }
}
