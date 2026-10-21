using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using LaMeridienneAPI.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Time.Testing;

namespace LaMeridienne.Tests;

[Collection(DatabaseCollection.Name)]
public sealed class CustomerAdminTests(PostgresFixture db)
{
    private const string Day = "2026-10-21";
    private readonly FakeTimeProvider _time = new(DateTimeOffset.Parse("2026-10-21T08:00:00Z"));

    private Task<ApiFactory> FactoryAsync() =>
        ApiFactory.CreateAsync(db.ConnectionStringFor("customer_admin_" + Guid.NewGuid().ToString("N")), time: _time);

    [Fact]
    public async Task US_E08_02_liste_paginee_par_nom_avec_recherche()
    {
        await using var factory = await FactoryAsync();
        var (client, _) = await factory.LoginAsync();
        foreach (var name in new[] { "Durand", "Élise", "Albert", "Dupont" })
            await client.AddEntryAsync("credits", Day, new { customerName = name, amount = 1 });

        var page2 = await client.GetFromJsonAsync<CustomerPageResponse>("/api/customers/list?page=2&pageSize=2");
        var search = await client.GetFromJsonAsync<CustomerPageResponse>("/api/customers/list?search=DU");
        var tooBig = await client.GetAsync("/api/customers/list?pageSize=101");

        Assert.Equal(4, page2!.Total);
        Assert.Equal(["Durand", "Élise"], page2.Items.Select(c => c.Name));
        Assert.Equal(["Dupont", "Durand"], search!.Items.Select(c => c.Name));
        Assert.Equal(["pageSize"], (await tooBig.ValidationProblemAsync()).Errors.Keys);
    }

    [Fact]
    public async Task US_E08_02_renommer_un_client_renomme_toutes_ses_lignes()
    {
        await using var factory = await FactoryAsync();
        var (client, _) = await factory.LoginAsync();
        var credit = await client.AddEntryAsync("credits", "2026-10-01", new { customerName = "Dupond", amount = 3 });
        await client.AddEntryAsync("refunds", Day, new { customerId = credit.Customer.Id, amount = 2 });

        var response = await client.PutAsJsonAsync($"/api/customers/{credit.Customer.Id}", new { name = " Dupont ", phone = "06 12" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var renamed = (await response.Content.ReadFromJsonAsync<CustomerResponse>())!;
        Assert.Equal(("Dupont", "06 12"), (renamed.Name, renamed.Phone));
        Assert.Equal("Dupont", (await client.SheetAsync("2026-10-01")).Credits.Single().Customer.Name);
        Assert.Equal("Dupont", (await client.SheetAsync(Day)).Refunds.Single().Customer.Name);
    }

    [Fact]
    public async Task US_E08_02_un_nom_porte_par_un_autre_client_est_refuse_et_la_fusion_proposee()
    {
        await using var factory = await FactoryAsync();
        var (client, _) = await factory.LoginAsync();
        var dupont = await client.AddEntryAsync("credits", Day, new { customerName = "Dupont", amount = 1 });
        var dupond = await client.AddEntryAsync("credits", Day, new { customerName = "Dupond", amount = 1 });

        var response = await client.PutAsJsonAsync($"/api/customers/{dupond.Customer.Id}", new { name = "DUPONT" });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        var conflicting = ((JsonElement)problem!.Extensions["conflictingCustomer"]!).Deserialize<CustomerResponse>(JsonSerializerOptions.Web);
        Assert.Equal((dupont.Customer.Id, "Dupont"), (conflicting!.Id, conflicting.Name));
    }

    [Fact]
    public async Task US_E08_02_fusionner_B_dans_A_deplace_ses_lignes_et_retire_B_de_l_autocompletion()
    {
        await using var factory = await FactoryAsync();
        var (client, _) = await factory.LoginAsync();
        var a = (await client.AddEntryAsync("credits", "2026-10-01", new { customerName = "Dupont", amount = 1 })).Customer;
        var bCredit = await client.AddEntryAsync("credits", Day, new { customerName = "Dupond", amount = 2 });
        var b = bCredit.Customer;
        await client.PutAsJsonAsync($"/api/customers/{b.Id}", new { name = "Dupond", phone = "0600" });
        await client.AddEntryAsync("refunds", Day, new { customerId = b.Id, amount = 3 });
        var deleted = await client.AddEntryAsync("credits", Day, new { customerId = b.Id, amount = 4 });
        await client.DeleteAsync($"/api/credits/{deleted.Id}");

        var response = await client.PostAsJsonAsync($"/api/customers/{a.Id}/merge", new { sourceId = b.Id });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("0600", (await response.Content.ReadFromJsonAsync<CustomerResponse>())!.Phone);
        var credits = await client.GetFromJsonAsync<CustomerCreditsResponse>($"/api/customers/{a.Id}/credits");
        Assert.Equal([2m, 1m], credits!.Credits.Select(c => c.Amount));
        Assert.Equal(a.Id, (await client.SheetAsync(Day)).Refunds.Single().Customer.Id);
        var search = await client.GetFromJsonAsync<List<CustomerResponse>>("/api/customers?search=dupo");
        Assert.Equal([a.Id], search!.Select(c => c.Id));
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/customers/{b.Id}/credits")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,
            (await client.PostAsJsonAsync($"/api/customers/{a.Id}/merge", new { sourceId = b.Id })).StatusCode);
    }

    [Fact]
    public async Task US_E08_02_apres_fusion_le_nom_de_B_est_libre()
    {
        await using var factory = await FactoryAsync();
        var (client, _) = await factory.LoginAsync();
        var a = (await client.AddEntryAsync("credits", Day, new { customerName = "Dupont", amount = 1 })).Customer;
        var b = (await client.AddEntryAsync("credits", Day, new { customerName = "Dupond", amount = 1 })).Customer;
        await client.PostAsJsonAsync($"/api/customers/{a.Id}/merge", new { sourceId = b.Id });

        var renamed = await client.PutAsJsonAsync($"/api/customers/{a.Id}", new { name = "Dupond" });

        Assert.Equal(HttpStatusCode.OK, renamed.StatusCode);
    }

    [Fact]
    public async Task US_E08_02_un_client_ne_peut_pas_etre_fusionne_avec_lui_meme()
    {
        await using var factory = await FactoryAsync();
        var (client, _) = await factory.LoginAsync();
        var a = (await client.AddEntryAsync("credits", Day, new { customerName = "Dupont", amount = 1 })).Customer;

        var self = await client.PostAsJsonAsync($"/api/customers/{a.Id}/merge", new { sourceId = a.Id });
        var unknown = await client.PostAsJsonAsync($"/api/customers/{a.Id}/merge", new { sourceId = Guid.NewGuid() });

        Assert.Equal(["sourceId"], (await self.ValidationProblemAsync()).Errors.Keys);
        Assert.Equal(HttpStatusCode.NotFound, unknown.StatusCode);
    }
}
