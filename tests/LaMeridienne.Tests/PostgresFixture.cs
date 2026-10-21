using Testcontainers.PostgreSql;

namespace LaMeridienne.Tests;

/// <summary>Un conteneur PostgreSQL partagé par toute la collection « Base ».</summary>
public sealed class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:17").Build();

    public string ConnectionString => _container.GetConnectionString();

    /// <summary>Chaîne vers une autre base du même serveur, pour isoler un test qui crée son propre schéma.</summary>
    public string ConnectionStringFor(string database) =>
        new Npgsql.NpgsqlConnectionStringBuilder(ConnectionString) { Database = database }.ConnectionString;

    public Task InitializeAsync() => _container.StartAsync();

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();
}

[CollectionDefinition(Name)]
public sealed class DatabaseCollection : ICollectionFixture<PostgresFixture>
{
    public const string Name = "Base";
}
