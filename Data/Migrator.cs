using Microsoft.EntityFrameworkCore;

namespace LaMeridienneAPI.Data;

/// <summary>Commande <c>--migrate</c> : applique les migrations puis rend la main, sans démarrer l'API.</summary>
public static class Migrator
{
    public const string Argument = "--migrate";

    public static async Task RunAsync(IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
    }
}
