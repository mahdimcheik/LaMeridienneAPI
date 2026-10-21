using System.Net;
using System.Net.Http.Json;
using LaMeridienneAPI.Data;
using LaMeridienneAPI.Dtos;
using LaMeridienneAPI.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;

namespace LaMeridienne.Tests;

[Collection(DatabaseCollection.Name)]
public sealed class NewspaperSalesTests(PostgresFixture db)
{
    private const string Day = "2026-10-21";
    private readonly FakeTimeProvider _time = new(DateTimeOffset.Parse("2026-10-21T08:00:00Z"));

    private Task<ApiFactory> FactoryAsync() =>
        ApiFactory.CreateAsync(db.ConnectionStringFor("sales_" + Guid.NewGuid().ToString("N")), time: _time);

    private static async Task<int> QuantityAsync(HttpClient client, string date, Guid newspaperId) =>
        (await client.SheetAsync(date)).Newspapers.Single(n => n.NewspaperId == newspaperId).Quantity;

    private static async Task<NewspaperSaleResponse> SaleAsync(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<NewspaperSaleResponse>())!;
    }

    [Fact]
    public async Task US_E05_01_chaque_journal_actif_est_a_0_par_defaut_dans_l_ordre()
    {
        await using var factory = await FactoryAsync();
        var (client, _) = await factory.LoginAsync();

        var sheet = await client.SheetAsync(Day);

        Assert.False(sheet.Exists);
        Assert.Equal([("Sud Ouest", 0), ("Le Républicain", 0)], sheet.Newspapers.Select(n => (n.Name, n.Quantity)));
    }

    [Fact]
    public async Task R_09_vingt_increments_paralleles_donnent_plus_20()
    {
        await using var factory = await FactoryAsync();
        var (marie, _) = await factory.LoginAsync(0);
        var (paul, _) = await factory.LoginAsync(1);
        await SaleAsync(await marie.SetQuantityAsync(Day, Newspaper.SudOuestId, 5));

        var responses = await Task.WhenAll(Enumerable.Range(0, 20)
            .Select(i => (i % 2 == 0 ? marie : paul).IncrementAsync(Day, Newspaper.SudOuestId, 1)));

        Assert.All(responses, r => Assert.Equal(HttpStatusCode.OK, r.StatusCode));
        Assert.Equal(25, await QuantityAsync(marie, Day, Newspaper.SudOuestId));
    }

    [Fact]
    public async Task R_09_des_premiers_increments_simultanes_sur_une_feuille_absente_s_additionnent()
    {
        await using var factory = await FactoryAsync();
        var (client, _) = await factory.LoginAsync();

        var responses = await Task.WhenAll(Enumerable.Range(0, 10).Select(_ => client.IncrementAsync(Day, Newspaper.LeRepublicainId, 1)));

        Assert.All(responses, r => Assert.Equal(HttpStatusCode.OK, r.StatusCode));
        Assert.Equal(10, await QuantityAsync(client, Day, Newspaper.LeRepublicainId));
    }

    [Fact]
    public async Task R_09_un_moins_1_a_0_est_ignore()
    {
        await using var factory = await FactoryAsync();
        var (client, _) = await factory.LoginAsync();
        await client.IncrementAsync(Day, Newspaper.SudOuestId, 1);

        var one = await SaleAsync(await client.IncrementAsync(Day, Newspaper.SudOuestId, -1));
        var zero = await SaleAsync(await client.IncrementAsync(Day, Newspaper.SudOuestId, -1));

        Assert.Equal((0, 0), (one.Quantity, zero.Quantity));
    }

    [Fact]
    public async Task US_E05_01_la_saisie_directe_remplace_la_quantite()
    {
        await using var factory = await FactoryAsync();
        var (client, _) = await factory.LoginAsync();
        await client.IncrementAsync(Day, Newspaper.SudOuestId, 1);

        var sale = await SaleAsync(await client.SetQuantityAsync(Day, Newspaper.SudOuestId, 14));

        Assert.Equal((Newspaper.SudOuestId, "Sud Ouest", true, 14), (sale.NewspaperId, sale.Name, sale.IsActive, sale.Quantity));
        Assert.Equal(14, await QuantityAsync(client, Day, Newspaper.SudOuestId));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(1000)]
    [InlineData(1.5)]
    public async Task US_E05_01_une_quantite_negative_decimale_ou_au_dela_de_999_est_refusee(object quantity)
    {
        await using var factory = await FactoryAsync();
        var (client, _) = await factory.LoginAsync();
        await client.SetQuantityAsync(Day, Newspaper.SudOuestId, 3);

        var response = await client.SetQuantityAsync(Day, Newspaper.SudOuestId, quantity);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(3, await QuantityAsync(client, Day, Newspaper.SudOuestId));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    [InlineData(-5)]
    public async Task R_09_un_increment_autre_que_plus_ou_moins_1_est_refuse(int delta)
    {
        await using var factory = await FactoryAsync();
        var (client, _) = await factory.LoginAsync();

        var problem = await (await client.IncrementAsync(Day, Newspaper.SudOuestId, delta)).ValidationProblemAsync();

        Assert.Equal(["Delta"], problem.Errors.Keys, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task US_E05_01_plus_1_a_999_est_ignore()
    {
        await using var factory = await FactoryAsync();
        var (client, _) = await factory.LoginAsync();
        await client.SetQuantityAsync(Day, Newspaper.SudOuestId, 999);

        var sale = await SaleAsync(await client.IncrementAsync(Day, Newspaper.SudOuestId, 1));

        Assert.Equal(999, sale.Quantity);
    }

    [Fact]
    public async Task US_E05_01_un_journal_inconnu_ou_une_date_invalide_sont_refuses()
    {
        await using var factory = await FactoryAsync();
        var (client, _) = await factory.LoginAsync();

        var unknown = await client.IncrementAsync(Day, Guid.NewGuid(), 1);
        var badDate = await client.IncrementAsync("21-10-2026", Newspaper.SudOuestId, 1);

        Assert.Equal(HttpStatusCode.NotFound, unknown.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, badDate.StatusCode);
    }

    [Fact]
    public async Task US_E05_02_un_journal_desactive_reste_sur_les_feuilles_ou_il_a_des_ventes()
    {
        await using var factory = await FactoryAsync();
        var (client, _) = await factory.LoginAsync();
        await client.SetQuantityAsync("2026-10-01", Newspaper.LeRepublicainId, 9);
        await client.GetAsync("/api/sheets/today");

        await client.PutAsJsonAsync($"/api/newspapers/{Newspaper.LeRepublicainId}", new { name = "Le Républicain", isActive = false });

        var withSales = (await client.SheetAsync("2026-10-01")).Newspapers;
        var withoutSales = (await client.SheetAsync(Day)).Newspapers;
        Assert.Equal([("Sud Ouest", true, 0), ("Le Républicain", false, 9)], withSales.Select(n => (n.Name, n.IsActive, n.Quantity)));
        Assert.Equal(["Sud Ouest"], withoutSales.Select(n => n.Name));
    }

    [Fact]
    public async Task US_E05_02_un_journal_ajoute_apres_le_jour_consulte_y_apparait_a_0_et_reste_saisissable()
    {
        await using var factory = await FactoryAsync();
        var (client, _) = await factory.LoginAsync();
        await client.SetQuantityAsync("2026-10-01", Newspaper.SudOuestId, 4);

        var equipe = await client.AddNewspaperAsync("L'Équipe");
        var before = await QuantityAsync(client, "2026-10-01", equipe.Id);
        await SaleAsync(await client.IncrementAsync("2026-10-01", equipe.Id, 1));

        Assert.Equal(0, before);
        Assert.Equal(1, await QuantityAsync(client, "2026-10-01", equipe.Id));
    }

    [Fact]
    public async Task US_E09_03_la_vente_garde_qui_l_a_saisie_et_qui_l_a_modifiee()
    {
        await using var factory = await FactoryAsync();
        var (marie, marieAuth) = await factory.LoginAsync(0);
        var (paul, paulAuth) = await factory.LoginAsync(1);

        await marie.IncrementAsync(Day, Newspaper.SudOuestId, 1);
        _time.Advance(TimeSpan.FromMinutes(5));
        await paul.IncrementAsync(Day, Newspaper.SudOuestId, 1);

        await using var scope = factory.Services.CreateAsyncScope();
        var sale = await scope.ServiceProvider.GetRequiredService<AppDbContext>().NewspaperSales.SingleAsync();
        Assert.Equal((marieAuth.User.Id, paulAuth.User.Id), (sale.CreatedById, sale.UpdatedById));
        Assert.Equal(DateTimeOffset.Parse("2026-10-21T08:00:00Z"), sale.CreatedAt);
        Assert.Equal(DateTimeOffset.Parse("2026-10-21T08:05:00Z"), sale.UpdatedAt);
    }
}
