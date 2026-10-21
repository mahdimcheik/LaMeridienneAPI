using System.Net;
using System.Net.Http.Json;
using LaMeridienneAPI.Dtos;

namespace LaMeridienne.Tests;

[Collection(DatabaseCollection.Name)]
public sealed class LayoutTests(PostgresFixture db)
{
    private const string Path = "/api/account/layout";

    private Task<ApiFactory> FactoryAsync() =>
        ApiFactory.CreateAsync(db.ConnectionStringFor("layout_" + Guid.NewGuid().ToString("N")));

    [Fact]
    public async Task R_07_sans_disposition_enregistree_la_disposition_par_defaut_s_applique()
    {
        await using var factory = await FactoryAsync();
        var (client, _) = await factory.LoginAsync();

        var layout = await client.GetFromJsonAsync<LayoutDto>(Path);

        Assert.Equal((0.5, 0.5, 0.5, true), (layout!.TopSplit, layout.BottomSplit, layout.RowSplit, layout.IsDefault));
    }

    [Fact]
    public async Task US_E02_03_la_disposition_est_retrouvee_et_propre_a_chaque_utilisateur()
    {
        await using var factory = await FactoryAsync();
        var (marie, _) = await factory.LoginAsync(0);
        var (paul, _) = await factory.LoginAsync(1);

        var saved = await marie.PutAsJsonAsync(Path, new { topSplit = 0.7, bottomSplit = 0.15, rowSplit = 0.85 });
        await marie.PutAsJsonAsync(Path, new { topSplit = 0.6, bottomSplit = 0.15, rowSplit = 0.85 });

        Assert.Equal(HttpStatusCode.OK, saved.StatusCode);
        var (again, _) = await factory.LoginAsync(0);
        var layout = await again.GetFromJsonAsync<LayoutDto>(Path);
        Assert.Equal((0.6, 0.15, 0.85, false), (layout!.TopSplit, layout.BottomSplit, layout.RowSplit, layout.IsDefault));
        Assert.True((await paul.GetFromJsonAsync<LayoutDto>(Path))!.IsDefault);
    }

    [Theory]
    [InlineData("{\"topSplit\":0.1,\"bottomSplit\":0.5,\"rowSplit\":0.5}", "topSplit")]
    [InlineData("{\"topSplit\":0.5,\"bottomSplit\":0.9,\"rowSplit\":0.5}", "bottomSplit")]
    [InlineData("{\"topSplit\":0.5,\"bottomSplit\":0.5,\"rowSplit\":0}", "rowSplit")]
    [InlineData("{\"topSplit\":0.5,\"bottomSplit\":0.5}", "rowSplit")]
    public async Task US_E02_03_une_disposition_invalide_est_refusee_et_l_ancienne_reste(string body, string field)
    {
        await using var factory = await FactoryAsync();
        var (client, _) = await factory.LoginAsync();

        var response = await client.PutAsync(Path, new StringContent(body, System.Text.Encoding.UTF8, "application/json"));

        Assert.Equal([field], (await response.ValidationProblemAsync()).Errors.Keys);
        Assert.True((await client.GetFromJsonAsync<LayoutDto>(Path))!.IsDefault);
    }
}
