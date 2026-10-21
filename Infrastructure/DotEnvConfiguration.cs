using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration.EnvironmentVariables;
using Npgsql;

namespace LaMeridienneAPI.Infrastructure;

/// <summary>
/// Lit un fichier <c>.env</c> à clés plates (<c>JWT_KEY</c>, <c>POSTGRES_PASSWORD</c>, <c>SEED_USER_0_NAME</c>…)
/// et les expose sous les clés de configuration de l'application (<c>Jwt:Key</c>, <c>ConnectionStrings:Default</c>…).
/// Fichier absent : aucune clé.
/// </summary>
public sealed partial class DotEnvConfigurationProvider(string path) : ConfigurationProvider
{
    public override void Load() =>
        Data = File.Exists(path)
            ? Translate(Parse(File.ReadAllLines(path)))
            : new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

    public static Dictionary<string, string> Parse(IEnumerable<string> lines)
    {
        var values = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var raw in lines)
        {
            var line = raw.Trim();
            if (line.Length == 0 || line.StartsWith('#')) continue;
            if (line.StartsWith("export ", StringComparison.Ordinal)) line = line[7..].TrimStart();
            var eq = line.IndexOf('=');
            if (eq <= 0) continue;
            var key = line[..eq].Trim();
            var value = line[(eq + 1)..].Trim();
            if (value.Length >= 2 && (value[0] == '"' || value[0] == '\'') && value[^1] == value[0])
                value = value[1..^1];
            values[key] = value;
        }
        return values;
    }

    public static Dictionary<string, string?> Translate(IReadOnlyDictionary<string, string> env)
    {
        var data = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        void Set(string flat, string key)
        {
            if (env.TryGetValue(flat, out var v) && v.Length > 0) data[key] = v;
        }

        if (env.TryGetValue("POSTGRES_HOST", out var host) && host.Length > 0)
        {
            var cs = new NpgsqlConnectionStringBuilder
            {
                Host = host,
                Port = env.TryGetValue("POSTGRES_PORT", out var port) && port.Length > 0 ? int.Parse(port) : 5432,
                Database = env.GetValueOrDefault("POSTGRES_DB"),
                Username = env.GetValueOrDefault("POSTGRES_USER"),
                Password = env.GetValueOrDefault("POSTGRES_PASSWORD"),
                GssEncryptionMode = GssEncryptionMode.Disable,
            };
            data["ConnectionStrings:Default"] = cs.ConnectionString;
        }

        Set("JWT_KEY", "Jwt:Key");
        Set("JWT_ISSUER", "Jwt:Issuer");
        Set("JWT_AUDIENCE", "Jwt:Audience");
        Set("SHOP_TIMEZONE", "Shop:TimeZone");

        foreach (var key in env.Keys)
        {
            var m = SeedKey().Match(key);
            if (!m.Success) continue;
            var field = m.Groups[2].Value switch { "NAME" => "UserName", "DISPLAY" => "DisplayName", _ => "Password" };
            Set(key, $"Seed:Users:{m.Groups[1].Value}:{field}");
        }

        if (env.TryGetValue("CORS_ORIGINS", out var origins))
        {
            var list = origins.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            for (var i = 0; i < list.Length; i++) data[$"Cors:Origins:{i}"] = list[i];
        }

        return data;
    }

    [GeneratedRegex(@"^SEED_USER_(\d+)_(NAME|DISPLAY|PASSWORD)$")]
    private static partial Regex SeedKey();
}

public sealed class DotEnvConfigurationSource(string path) : IConfigurationSource
{
    public IConfigurationProvider Build(IConfigurationBuilder builder) => new DotEnvConfigurationProvider(path);
}

public static class DotEnvConfigurationExtensions
{
    /// <summary>Juste sous les variables d'environnement : elles, la ligne de commande et les tests l'emportent.</summary>
    public static ConfigurationManager AddDotEnvFile(this ConfigurationManager configuration, string path)
    {
        IList<IConfigurationSource> sources = ((IConfigurationBuilder)configuration).Sources;
        var index = sources.ToList().FindLastIndex(s => s is EnvironmentVariablesConfigurationSource { Prefix: null or "" });
        var source = new DotEnvConfigurationSource(path);
        if (index < 0) sources.Add(source);
        else sources.Insert(index, source);
        return configuration;
    }
}
