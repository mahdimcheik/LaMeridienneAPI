using System.Net;

namespace LaMeridienne.Tests;

[Collection(DatabaseCollection.Name)]
public sealed class HealthTests(PostgresFixture db)
{
    [Fact]
    public async Task Sante_repond_healthy_avec_la_base()
    {
        await using var factory = await ApiFactory.CreateAsync(db.ConnectionString);

        var response = await factory.CreateClient().GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Sante_repond_503_sans_base()
    {
        await using var factory = new ApiFactory("Host=127.0.0.1;Port=1;Database=absente;Username=x;Password=x;Timeout=2")
        {
            WithSeeder = false,
        };

        var response = await factory.CreateClient().GetAsync("/health");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }
}
