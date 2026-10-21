using LaMeridienneAPI.Data;
using LaMeridienneAPI.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace LaMeridienneAPI.Auth;

/// <summary>
/// Crée au démarrage les comptes de <c>SEED_USER_n_*</c> (<c>Seed:Users:n</c>) qui n'existent pas (ADR-0003, US-E01-04).
/// Un compte existant n'est jamais modifié ; une configuration incomplète sur une base sans comptes
/// empêche le démarrage.
/// </summary>
public sealed class UserSeeder(
    IServiceScopeFactory scopes,
    IConfiguration configuration,
    IPasswordHasher<User> hasher,
    ILogger<UserSeeder> logger
) : IHostedService
{
    public const int RequiredAccounts = 3;

    public async Task StartAsync(CancellationToken ct)
    {
        await using var scope = scopes.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var existing = (await db.Users.Select(u => u.UserName).ToListAsync(ct)).ToHashSet();

        var section = configuration.GetSection("Seed:Users");
        var configured = section
            .GetChildren()
            .Select(c => int.TryParse(c.Key, out var i) ? i + 1 : 0)
            .DefaultIfEmpty(0)
            .Max();
        var slots = existing.Count == 0 ? Math.Max(RequiredAccounts, configured) : configured;

        var problems = new List<string>();
        var created = new List<User>();
        for (var i = 0; i < slots; i++)
        {
            var userName = section[$"{i}:UserName"]?.Trim();
            var password = section[$"{i}:Password"];
            if (!string.IsNullOrEmpty(userName) && existing.Contains(userName))
                continue;
            if (
                existing.Count > 0
                && string.IsNullOrEmpty(userName)
                && string.IsNullOrEmpty(password)
            )
                continue;

            var before = problems.Count;
            if (string.IsNullOrEmpty(userName))
                problems.Add($"SEED_USER_{i}_NAME est absente.");
            else if (created.Any(u => u.UserName == userName))
                problems.Add($"SEED_USER_{i}_NAME « {userName} » est en double.");
            if (string.IsNullOrEmpty(password))
                problems.Add($"SEED_USER_{i}_PASSWORD est absente.");
            else if (password.Length < AuthService.MinPasswordLength)
                problems.Add(
                    $"SEED_USER_{i}_PASSWORD doit contenir au moins {AuthService.MinPasswordLength} caractères."
                );
            if (problems.Count > before)
                continue;

            var displayName = section[$"{i}:DisplayName"]?.Trim();
            var user = new User
            {
                UserName = userName!,
                DisplayName = string.IsNullOrEmpty(displayName) ? userName! : displayName,
            };
            user.PasswordHash = hasher.HashPassword(user, password!);
            created.Add(user);
        }

        if (problems.Count > 0)
            throw new InvalidOperationException(
                "Comptes initiaux incomplets : " + string.Join(" ", problems)
            );
        if (created.Count == 0)
            return;

        db.Users.AddRange(created);
        await db.SaveChangesAsync(ct);
        logger.LogInformation(
            "Comptes initiaux créés : {UserNames}",
            string.Join(", ", created.Select(u => u.UserName))
        );
    }

    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;
}
