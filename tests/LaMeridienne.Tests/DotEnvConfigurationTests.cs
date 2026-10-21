using System.Net;
using LaMeridienneAPI.Data;
using LaMeridienneAPI.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace LaMeridienne.Tests;

public sealed class DotEnvParsingTests
{
    [Fact]
    public void Ignore_commentaires_et_lignes_vides_et_retire_les_guillemets()
    {
        var values = DotEnvConfigurationProvider.Parse(
        [
            "# commentaire",
            "",
            "JWT_KEY=abc=def",
            "SEED_USER_0_DISPLAY=\"Léa Martin\"",
            "export SHOP_TIMEZONE='Europe/Paris'",
            "sans-egal",
        ]);

        Assert.Equal(3, values.Count);
        Assert.Equal("abc=def", values["JWT_KEY"]);
        Assert.Equal("Léa Martin", values["SEED_USER_0_DISPLAY"]);
        Assert.Equal("Europe/Paris", values["SHOP_TIMEZONE"]);
    }

    [Fact]
    public void Traduit_les_cles_plates_en_configuration()
    {
        var data = DotEnvConfigurationProvider.Translate(new Dictionary<string, string>
        {
            ["POSTGRES_HOST"] = "postgres",
            ["POSTGRES_DB"] = "lameridienne",
            ["POSTGRES_USER"] = "u",
            ["POSTGRES_PASSWORD"] = "p;w",
            ["JWT_KEY"] = "k",
            ["JWT_ISSUER"] = "",
            ["SEED_USER_1_NAME"] = "paul",
            ["SEED_USER_1_DISPLAY"] = "Paul",
            ["SEED_USER_1_PASSWORD"] = "secret",
            ["CORS_ORIGINS"] = "https://a.fr, https://b.fr",
            ["AUTRE"] = "x",
        });

        var cs = new NpgsqlConnectionStringBuilder(data["ConnectionStrings:Default"]);
        Assert.Equal(("postgres", 5432, "lameridienne", "u", "p;w"), (cs.Host, cs.Port, cs.Database, cs.Username, cs.Password));
        Assert.Equal(GssEncryptionMode.Disable, cs.GssEncryptionMode);
        Assert.Equal("k", data["Jwt:Key"]);
        Assert.False(data.ContainsKey("Jwt:Issuer"));
        Assert.Equal("paul", data["Seed:Users:1:UserName"]);
        Assert.Equal("Paul", data["Seed:Users:1:DisplayName"]);
        Assert.Equal("secret", data["Seed:Users:1:Password"]);
        Assert.Equal("https://a.fr", data["Cors:Origins:0"]);
        Assert.Equal("https://b.fr", data["Cors:Origins:1"]);
        Assert.Equal(7, data.Count);
    }

    [Fact]
    public void Sans_hote_pas_de_chaine_et_fichier_absent_sans_cle()
    {
        Assert.Empty(DotEnvConfigurationProvider.Translate(new Dictionary<string, string> { ["POSTGRES_PASSWORD"] = "p" }));

        var provider = new DotEnvConfigurationProvider(Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".env"));
        provider.Load();
        Assert.Empty(provider.GetChildKeys([], null));
    }
}

[Collection(DatabaseCollection.Name)]
public sealed class DotEnvStartupTests(PostgresFixture db)
{
    [Fact]
    public async Task L_api_demarre_avec_toute_sa_configuration_dans_le_fichier_env()
    {
        var cs = new NpgsqlConnectionStringBuilder(db.ConnectionStringFor("dotenv_" + Guid.NewGuid().ToString("N")));

        var file = Path.Combine(Path.GetTempPath(), $"lameridienne-{Guid.NewGuid():N}.env");
        var lines = new List<string>
        {
            $"POSTGRES_HOST={cs.Host}", $"POSTGRES_PORT={cs.Port}", $"POSTGRES_DB={cs.Database}",
            $"POSTGRES_USER={cs.Username}", $"POSTGRES_PASSWORD={cs.Password}",
            "JWT_KEY=\"cle-de-test-suffisamment-longue-pour-hs256\"",
        };
        for (var i = 0; i < ApiFactory.Accounts.Length; i++)
            lines.AddRange([$"SEED_USER_{i}_NAME={ApiFactory.Accounts[i].UserName}", $"SEED_USER_{i}_PASSWORD={ApiFactory.Accounts[i].Password}"]);
        await File.WriteAllLinesAsync(file, lines);

        try
        {
            await using var factory = new ApiFactory("") { EnvFile = file };
            factory.Settings.Clear();

            Assert.Equal(HttpStatusCode.OK, (await factory.CreateClient().GetAsync("/health")).StatusCode);
            var (_, auth) = await factory.LoginAsync(1);
            Assert.Equal("paul", auth.User.UserName);
        }
        finally
        {
            File.Delete(file);
        }
    }
}
