using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Options;

namespace CuriosityStack.Mcp.Core.Storage;

public sealed class SqliteOptions
{
    public string DatabasePath { get; set; } = Path.Combine("data", "personal-mcp.db");
}

public interface ISqliteStore
{
    Task<int> ExecuteAsync(string sql, IReadOnlyDictionary<string, object?>? parameters = null, CancellationToken cancellationToken = default);
    Task<T?> QuerySingleAsync<T>(string sql, Func<SqliteDataReader, T> mapper, IReadOnlyDictionary<string, object?>? parameters = null, CancellationToken cancellationToken = default);
    Task<List<T>> QueryAsync<T>(string sql, Func<SqliteDataReader, T> mapper, IReadOnlyDictionary<string, object?>? parameters = null, CancellationToken cancellationToken = default);
}

public sealed class SqliteStore : ISqliteStore
{
    private readonly SqliteOptions _options;

    public SqliteStore(IOptions<SqliteOptions> options)
    {
        _options = options.Value;
        var directory = Path.GetDirectoryName(_options.DatabasePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }

    public async Task<int> ExecuteAsync(string sql, IReadOnlyDictionary<string, object?>? parameters = null, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqliteConnection(BuildConnectionString());
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        BindParameters(command, parameters);
        return await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<T?> QuerySingleAsync<T>(string sql, Func<SqliteDataReader, T> mapper, IReadOnlyDictionary<string, object?>? parameters = null, CancellationToken cancellationToken = default)
    {
        var items = await QueryAsync(sql, mapper, parameters, cancellationToken);
        return items.Count == 0 ? default : items[0];
    }

    public async Task<List<T>> QueryAsync<T>(string sql, Func<SqliteDataReader, T> mapper, IReadOnlyDictionary<string, object?>? parameters = null, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqliteConnection(BuildConnectionString());
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        BindParameters(command, parameters);

        var results = new List<T>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(mapper(reader));
        }

        return results;
    }

    private string BuildConnectionString()
    {
        return new SqliteConnectionStringBuilder
        {
            DataSource = _options.DatabasePath,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Cache = SqliteCacheMode.Shared,
            ForeignKeys = true,
        }.ToString();
    }

    private static void BindParameters(SqliteCommand command, IReadOnlyDictionary<string, object?>? parameters)
    {
        if (parameters is null)
        {
            return;
        }

        foreach (var (key, value) in parameters)
        {
            command.Parameters.AddWithValue($"@{key}", value ?? DBNull.Value);
        }
    }
}
