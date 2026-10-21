using Microsoft.EntityFrameworkCore;

namespace LaMeridienneAPI.Data;

/// <summary>
/// Applique les migrations en attente à chaque démarrage, dans tous les environnements ; crée la base
/// si elle n'existe pas. Enregistré avant <c>UserSeeder</c> : les services hébergés démarrent dans l'ordre.
/// </summary>
public sealed class Migrator(IServiceScopeFactory scopes) : IHostedService
{
    public async Task StartAsync(CancellationToken ct)
    {
        await using var scope = scopes.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.MigrateAsync(ct);
    }

    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;
}
