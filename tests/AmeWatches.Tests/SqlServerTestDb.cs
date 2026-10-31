using AmeWatches.Api.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace AmeWatches.Tests;

public sealed class SqlServerFactAttribute : FactAttribute
{
    public const string EnvironmentVariable = "AME_TEST_SQL";

    public SqlServerFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(EnvironmentVariable)))
            Skip = $"Vendos {EnvironmentVariable} (lidhja me SQL Server) për ta ekzekutuar.";
    }
}

public sealed class SqlServerTestDb : IAsyncDisposable
{
    private readonly string _masterConnection;
    private readonly string _databaseName = $"AmeWatches_Test_{Guid.NewGuid():N}";

    public string ConnectionString { get; }

    public SqlServerTestDb()
    {
        _masterConnection = Environment.GetEnvironmentVariable(SqlServerFactAttribute.EnvironmentVariable)!;
        ConnectionString = new SqlConnectionStringBuilder(_masterConnection) { InitialCatalog = _databaseName }.ConnectionString;
    }

    public AppDbContext NewContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(ConnectionString).Options;
        return new AppDbContext(options);
    }

    public async Task CreateAsync()
    {
        await using var context = NewContext();
        await context.Database.EnsureCreatedAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await using var connection = new SqlConnection(_masterConnection);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = $"ALTER DATABASE [{_databaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{_databaseName}];";
        await command.ExecuteNonQueryAsync();
    }
}
