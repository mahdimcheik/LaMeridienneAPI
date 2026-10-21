using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;

namespace LaMeridienne.Tests;

[Collection(DatabaseCollection.Name)]
public sealed class AccountTests(PostgresFixture db)
{
    private const string Path = "/api/account/password";

    private Task<ApiFactory> FactoryAsync() =>
        ApiFactory.CreateAsync(db.ConnectionStringFor("account_" + Guid.NewGuid().ToString("N")));

    private static async Task<ValidationProblemDetails> ValidationAsync(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<ValidationProblemDetails>())!;
    }

    [Fact]
    public async Task US_E08_03_un_mot_de_passe_actuel_errone_est_refuse()
    {
        await using var factory = await FactoryAsync();
        var (client, _) = await factory.LoginAsync();

        var problem = await ValidationAsync(await client.PutAsJsonAsync(Path,
            new { currentPassword = "pas-le-bon-mot-de-passe", newPassword = "un-nouveau-mot-de-passe" }));

        Assert.Equal(["currentPassword"], problem.Errors.Keys);
    }

    [Fact]
    public async Task US_E08_03_R_14_un_mot_de_passe_de_moins_de_12_caracteres_est_refuse()
    {
        await using var factory = await FactoryAsync();
        var (client, _) = await factory.LoginAsync();

        var problem = await ValidationAsync(await client.PutAsJsonAsync(Path,
            new { currentPassword = ApiFactory.Accounts[0].Password, newPassword = "trop-court" }));

        Assert.Equal(["newPassword"], problem.Errors.Keys);
        Assert.Contains("12 caractères", problem.Errors["newPassword"].Single());
    }

    [Fact]
    public async Task US_E08_03_les_champs_manquants_sont_signales_par_leur_nom_json()
    {
        await using var factory = await FactoryAsync();
        var (client, _) = await factory.LoginAsync();

        var problem = await ValidationAsync(await client.PutAsJsonAsync(Path, new { }));

        Assert.Equal(["currentPassword", "newPassword"], problem.Errors.Keys.Order());
    }

    [Fact]
    public async Task US_E08_03_le_changement_revoque_les_autres_sessions_et_garde_la_courante()
    {
        await using var factory = await FactoryAsync();
        var raw = factory.RawClient();
        var (access, current) = await (await raw.LoginAsync()).TokensAsync();
        var (_, other) = await (await raw.LoginAsync()).TokensAsync();
        const string newPassword = "un-tout-nouveau-mot-de-passe";

        var change = await raw.SendWithBearerAsync(HttpMethod.Put, Path, access,
            new { currentPassword = ApiFactory.Accounts[0].Password, newPassword });

        Assert.Equal(HttpStatusCode.NoContent, change.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await raw.RefreshAsync(other)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await raw.RefreshAsync(current)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await raw.LoginAsync()).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await raw.LoginAsync("marie", newPassword)).StatusCode);
    }
}
