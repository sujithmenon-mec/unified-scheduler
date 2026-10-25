using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using ServiceScheduler.Api.Infrastructure;

namespace ServiceScheduler.Tests;

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class PostgreSqlTestCollection : ICollectionFixture<PostgreSqlTestFixture>
{
    public const string Name = "PostgreSQL integration tests";
}

public sealed class PostgreSqlTestFixture : IAsyncLifetime
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public PostgreSqlTestFixture()
    {
        var settingsPath = Path.Combine(AppContext.BaseDirectory, "appsettings.Development.json");
        var configuration = new ConfigurationBuilder()
            .AddJsonFile(settingsPath, optional: false)
            .Build();
        ConnectionString = configuration.GetConnectionString("SchedulerTestDb")
            ?? throw new InvalidOperationException("Connection string 'SchedulerTestDb' was not found.");
        if (!string.Equals(
                new NpgsqlConnectionStringBuilder(ConnectionString).Database,
                "scheduler_test",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Integration tests must use the scheduler_test database.");
        }

        var previousConnectionString = Environment.GetEnvironmentVariable("ConnectionStrings__SchedulerDb");
        Environment.SetEnvironmentVariable("ConnectionStrings__SchedulerDb", ConnectionString);
        try
        {
            _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
                builder.UseEnvironment("Testing"));
            _client = _factory.CreateClient();
        }
        finally
        {
            Environment.SetEnvironmentVariable("ConnectionStrings__SchedulerDb", previousConnectionString);
        }
    }

    public string ConnectionString { get; }
    public HttpClient Client => _client;
    public IServiceProvider Services => _factory.Services;

    public async Task InitializeAsync()
    {
        await using var scope = Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SchedulerDbContext>();
        await dbContext.Database.MigrateAsync();

        var seedPath = Path.Combine(AppContext.BaseDirectory, "db", "02_seed.sql");
        await dbContext.Database.ExecuteSqlRawAsync(await File.ReadAllTextAsync(seedPath));
    }

    public Task DisposeAsync()
    {
        _client.Dispose();
        _factory.Dispose();
        return Task.CompletedTask;
    }
}
