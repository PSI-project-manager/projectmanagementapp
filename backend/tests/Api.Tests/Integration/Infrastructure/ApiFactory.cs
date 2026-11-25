using Api.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using Xunit;

namespace Api.Tests.Integration.Infrastructure;

// boots the real API in memory against a throwaway PostgreSQL container, so the tests
// go through the same middleware, auth, exception handler, EF migrations and constraints
// as production
public class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string SigningKey = "integration-tests-signing-key-0123456789-abcdefghijklmnop";

    private readonly PostgreSqlContainer database = new PostgreSqlBuilder("postgres:18").Build();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // anything but Development, so Program.cs doesn't run its own migrate + dev seed
        builder.UseEnvironment("Testing");
        builder.UseSetting("Db:ConnString", database.GetConnectionString());
        builder.UseSetting("Jwt:SigningKey", SigningKey);
    }

    public async Task InitializeAsync()
    {
        await database.StartAsync();

        using var scope = Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.MigrateAsync();
    }

    // wipes every table (but keeps the schema) so each test starts from an empty database
    public async Task ResetDatabaseAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await db.Database.ExecuteSqlRawAsync(
            """
            TRUNCATE TABLE items, projectusers, projects, userroles, users, roles, itemtypes, statuses
            RESTART IDENTITY CASCADE;
            """
        );
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await base.DisposeAsync();
        await database.DisposeAsync();
    }
}

// every integration test class shares one container and runs one after another,
// because they all reset the same database
[CollectionDefinition(Name)]
public class IntegrationCollection : ICollectionFixture<ApiFactory>
{
    public const string Name = "Integration";
}
