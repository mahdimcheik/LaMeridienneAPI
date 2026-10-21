using System.Net;
using System.Net.Http.Json;
using LaMeridienneAPI.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LaMeridienne.Tests;

[Collection(DatabaseCollection.Name)]
public sealed class SeedTests(PostgresFixture db)
{
    private readonly string _connectionString = db.ConnectionStringFor("seed_" + Guid.NewGuid().ToString("N"));

    private static async Task<List<LaMeridienneAPI.Models.User>> UsersAsync(ApiFactory factory)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<AppDbContext>().Users.OrderBy(u => u.UserName).ToListAsync();
    }

    [Fact]
    public async Task US_E01_04_les_trois_comptes_existent_au_demarrage_avec_des_mots_de_passe_haches()
    {
        await using var factory = await ApiFactory.CreateAsync(_connectionString);

        var users = await UsersAsync(factory);

        Assert.Equal(["lea", "marie", "paul"], users.Select(u => u.UserName));
        Assert.Equal("Léa", users[0].DisplayName);
        Assert.All(users, u => Assert.DoesNotContain("mot-de-passe", u.PasswordHash));
    }

    [Fact]
    public async Task US_E01_04_un_redemarrage_ne_duplique_ni_n_ecrase_aucun_compte()
    {
        const string changed = "mot-de-passe-change-par-marie";
        await using (var first = await ApiFactory.CreateAsync(_connectionString))
        {
            var (client, _) = await first.LoginAsync();
            (await client.PutAsJsonAsync("/api/account/password",
                new { currentPassword = ApiFactory.Accounts[0].Password, newPassword = changed })).EnsureSuccessStatusCode();
        }

        await using var second = await ApiFactory.CreateAsync(_connectionString);

        Assert.Equal(3, (await UsersAsync(second)).Count);
        Assert.Equal(HttpStatusCode.OK, (await second.RawClient().LoginAsync("marie", changed)).StatusCode);
    }

    [Theory]
    [InlineData("Seed:Users:1:Password", "Seed__Users__1__Password est absente")]
    [InlineData("Seed:Users:2:UserName", "Seed__Users__2__UserName est absente")]
    public async Task US_E01_04_une_variable_manquante_empeche_le_demarrage_et_est_nommee(string removed, string expected)
    {
        await using var factory = await ApiFactory.CreateAsync(_connectionString, f => f.Settings.Remove(removed));

        var error = Record.Exception(() => factory.CreateClient());

        Assert.NotNull(error);
        Assert.Contains(expected, error.ToString());
    }

    [Fact]
    public async Task R_14_un_mot_de_passe_initial_trop_court_empeche_le_demarrage()
    {
        await using var factory = await ApiFactory.CreateAsync(_connectionString,
            f => f.Settings["Seed:Users:0:Password"] = "court");

        var error = Record.Exception(() => factory.CreateClient());

        Assert.Contains("Seed__Users__0__Password doit contenir au moins 12 caractères", error?.ToString());
    }

    [Fact]
    public async Task Une_cle_jwt_trop_courte_empeche_le_demarrage()
    {
        await using var factory = await ApiFactory.CreateAsync(_connectionString, f => f.Settings["Jwt:Key"] = "courte");

        var error = Record.Exception(() => factory.CreateClient());

        Assert.Contains("Jwt__Key", error?.ToString());
    }
}
