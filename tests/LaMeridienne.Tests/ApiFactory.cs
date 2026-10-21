using System.Net.Http.Headers;
using System.Net.Http.Json;
using LaMeridienneAPI.Auth;
using LaMeridienneAPI.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace LaMeridienne.Tests;

public class ApiFactory : WebApplicationFactory<Program>
{
    private readonly string _connectionString;

    /// <summary>Clients en HTTPS : le cookie de renouvellement est <c>Secure</c>.</summary>
    public ApiFactory(string connectionString)
    {
        _connectionString = connectionString;
        ClientOptions.BaseAddress = new Uri("https://localhost");
    }

    public static readonly (string UserName, string DisplayName, string Password)[] Accounts =
    [
        ("marie", "Marie", "mot-de-passe-marie"),
        ("paul", "Paul", "mot-de-passe-paul"),
        ("lea", "Léa", "mot-de-passe-lea"),
    ];

    /// <summary>Faux : la base est absente, le démarrage ne doit pas tenter de créer les comptes.</summary>
    public bool WithSeeder { get; init; } = true;

    public TimeProvider? Time { get; init; }

    public Dictionary<string, string?> Settings { get; } = DefaultSettings();

    /// <summary>Migre la base avant le démarrage de l'hôte, car les comptes sont créés au démarrage.</summary>
    public static async Task<ApiFactory> CreateAsync(string connectionString, Action<ApiFactory>? configure = null,
        TimeProvider? time = null)
    {
        await using (var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(connectionString).Options))
            await db.Database.MigrateAsync();
        var factory = new ApiFactory(connectionString) { Time = time };
        configure?.Invoke(factory);
        return factory;
    }

    private static Dictionary<string, string?> DefaultSettings()
    {
        var settings = new Dictionary<string, string?>
        {
            ["Jwt:Key"] = "cle-de-test-suffisamment-longue-pour-hs256",
            ["Jwt:Issuer"] = "LaMeridienneAPI",
            ["Jwt:Audience"] = "LaMeridienneClient",
        };
        for (var i = 0; i < Accounts.Length; i++)
        {
            settings[$"Seed:Users:{i}:UserName"] = Accounts[i].UserName;
            settings[$"Seed:Users:{i}:DisplayName"] = Accounts[i].DisplayName;
            settings[$"Seed:Users:{i}:Password"] = Accounts[i].Password;
        }
        return settings;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:Default", _connectionString);
        foreach (var (key, value) in Settings)
            builder.UseSetting(key, value);

        builder.ConfigureTestServices(services =>
        {
            if (!WithSeeder)
                foreach (var seeder in services.Where(d => d.ImplementationType == typeof(UserSeeder)).ToList())
                    services.Remove(seeder);
            if (Time is not null)
                services.Replace(ServiceDescriptor.Singleton(Time));
        });
    }

    /// <summary>Un client neuf (son propre bocal à cookies) connecté avec le compte donné.</summary>
    public async Task<(HttpClient Client, AuthResponseDto Auth)> LoginAsync(int account = 0)
    {
        var client = CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/login",
            new { userName = Accounts[account].UserName, password = Accounts[account].Password });
        response.EnsureSuccessStatusCode();
        var auth = (await response.Content.ReadFromJsonAsync<AuthResponseDto>())!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        return (client, auth);
    }
}

public sealed record UserDto(Guid Id, string UserName, string DisplayName);

public sealed record AuthResponseDto(string AccessToken, DateTimeOffset ExpiresAt, UserDto User);
