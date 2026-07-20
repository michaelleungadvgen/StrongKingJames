using Microsoft.EntityFrameworkCore;
using Pgvector.EntityFrameworkCore;
using StrongKingJames.Data;
using Testcontainers.PostgreSql;
using Xunit;

namespace StrongKingJames.Data.Tests;

public class DatabaseFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("pgvector/pgvector:pg16")
        .Build();

    public string ConnectionString => _container.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        await using var ctx = CreateContext();
        await ctx.Database.MigrateAsync();
    }

    public BibleDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<BibleDbContext>()
            .UseNpgsql(ConnectionString, o => o.UseVector())
            .Options;
        return new BibleDbContext(options);
    }

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();
}

[CollectionDefinition("db")]
public class DbCollection : ICollectionFixture<DatabaseFixture> { }
