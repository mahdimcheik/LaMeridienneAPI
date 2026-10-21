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

    /// <summary>Faux : la base est absente, le démarrage ne doit ni migrer ni créer les comptes.</summary>
    public bool WithDatabase { get; init; } = true;

    public TimeProvider? Time { get; init; }

    /// <summary>Vide : le <c>.env</c> de développement à la racine ne doit pas influer sur les tests.</summary>
    public string EnvFile { get; set; } = "";

    public Dictionary<string, string?> Settings { get; } = DefaultSettings();

    /// <summary>La base est créée et migrée au démarrage de l'hôte (<see cref="Migrator"/>).</summary>
    public static Task<ApiFactory> CreateAsync(string connectionString, Action<ApiFactory>? configure = null,
        TimeProvider? time = null)
    {
        var factory = new ApiFactory(connectionString) { Time = time };
        configure?.Invoke(factory);
        return Task.FromResult(factory);
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
        builder.UseSetting("ENV_FILE", EnvFile);
        if (_connectionString.Length > 0)
            builder.UseSetting("ConnectionStrings:Default", _connectionString);
        foreach (var (key, value) in Settings)
            builder.UseSetting(key, value);

        builder.ConfigureTestServices(services =>
        {
            if (!WithDatabase)
                foreach (var startup in services
                             .Where(d => d.ImplementationType == typeof(Migrator) || d.ImplementationType == typeof(UserSeeder))
                             .ToList())
                    services.Remove(startup);
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
