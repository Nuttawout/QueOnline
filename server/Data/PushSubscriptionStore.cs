using Microsoft.Data.Sqlite;
using QueOnline.Api.Models;

namespace QueOnline.Api.Data;

/// <summary>
/// Push subscriptions live in a local SQLite file, deliberately kept OUT of any branch's Que_lcb
/// SQL Server database — this app only ever reads Que_lcb (Queue_q), it never writes to it, and
/// subscription state is our own app's concern, not the legacy queue system's. One shared file for
/// every branch, scoped per-row by BranchSlug — a single device can watch queues at two different
/// branches at once, so the uniqueness key is (Endpoint, BranchSlug), not Endpoint alone.
/// </summary>
public interface IPushSubscriptionStore
{
    Task InitializeAsync();
    Task UpsertAsync(string branchSlug, string? hn, string? qNumber, PushSubscriptionPayloadDto subscription, CancellationToken ct);
    Task RemoveAsync(string branchSlug, string endpoint, CancellationToken ct);
    Task<IReadOnlyList<PushSubscriptionRecord>> GetAllForBranchAsync(string branchSlug, CancellationToken ct);
    Task UpdateLevelAsync(long id, int level, CancellationToken ct);
}

public sealed class PushSubscriptionStore : IPushSubscriptionStore
{
    private readonly string _connectionString;

    public PushSubscriptionStore(IConfiguration configuration)
    {
        var dbPath = configuration["PushSubscriptionDbPath"] ?? "App_Data/push_subscriptions.db";
        var fullPath = Path.IsPathRooted(dbPath) ? dbPath : Path.Combine(AppContext.BaseDirectory, "..", "..", "..", dbPath);
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(fullPath))!);
        _connectionString = $"Data Source={Path.GetFullPath(fullPath)}";
    }

    private SqliteConnection CreateConnection() => new(_connectionString);

    public async Task InitializeAsync()
    {
        using var connection = CreateConnection();
        await connection.OpenAsync();

        // Unlike AppSettingsStore, this table's pre-multi-branch shape (Endpoint UNIQUE alone, no
        // BranchSlug column) is safe to drop outright rather than migrate row-by-row: subscriptions
        // are ephemeral push-endpoint registrations the browser re-sends on its own, not owned data,
        // and the dev database's PushSubscriptions table was confirmed empty (0 rows) before this
        // was written. `CREATE TABLE IF NOT EXISTS` is a no-op against the old-shaped table, so it
        // must be dropped explicitly or every insert would fail on the missing BranchSlug column.
        var oldShapeCommand = connection.CreateCommand();
        oldShapeCommand.CommandText = "SELECT COUNT(*) FROM pragma_table_info('PushSubscriptions') WHERE name = 'BranchSlug';";
        var hasBranchSlugColumn = Convert.ToInt64(await oldShapeCommand.ExecuteScalarAsync()) > 0;

        if (!hasBranchSlugColumn)
        {
            var dropCommand = connection.CreateCommand();
            dropCommand.CommandText = "DROP TABLE IF EXISTS PushSubscriptions;";
            await dropCommand.ExecuteNonQueryAsync();
        }

        var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS PushSubscriptions (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                BranchSlug TEXT NOT NULL,
                Hn TEXT NULL,
                QNumber TEXT NULL,
                Endpoint TEXT NOT NULL,
                P256dh TEXT NOT NULL,
                Auth TEXT NOT NULL,
                LastLevel INTEGER NOT NULL DEFAULT 0,
                CreatedAt TEXT NOT NULL,
                UNIQUE (Endpoint, BranchSlug)
            );
            """;
        await command.ExecuteNonQueryAsync();
    }

    public async Task UpsertAsync(string branchSlug, string? hn, string? qNumber, PushSubscriptionPayloadDto subscription, CancellationToken ct)
    {
        using var connection = CreateConnection();
        await connection.OpenAsync(ct);
        var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO PushSubscriptions (BranchSlug, Hn, QNumber, Endpoint, P256dh, Auth, LastLevel, CreatedAt)
            VALUES ($branchSlug, $hn, $qnumber, $endpoint, $p256dh, $auth, 0, $createdAt)
            ON CONFLICT(Endpoint, BranchSlug) DO UPDATE SET
                Hn = excluded.Hn,
                QNumber = excluded.QNumber,
                P256dh = excluded.P256dh,
                Auth = excluded.Auth,
                LastLevel = 0;
            """;
        command.Parameters.AddWithValue("$branchSlug", branchSlug);
        command.Parameters.AddWithValue("$hn", (object?)hn ?? DBNull.Value);
        command.Parameters.AddWithValue("$qnumber", (object?)qNumber ?? DBNull.Value);
        command.Parameters.AddWithValue("$endpoint", subscription.Endpoint);
        command.Parameters.AddWithValue("$p256dh", subscription.Keys.P256dh);
        command.Parameters.AddWithValue("$auth", subscription.Keys.Auth);
        command.Parameters.AddWithValue("$createdAt", DateTime.UtcNow.ToString("O"));
        await command.ExecuteNonQueryAsync(ct);
    }

    public async Task RemoveAsync(string branchSlug, string endpoint, CancellationToken ct)
    {
        // Scoped by branch too, not just Endpoint — the same device/browser can hold a push
        // subscription for more than one branch (they share one origin), so an unscoped delete
        // here would silently kill a different branch's notifications for that same device.
        using var connection = CreateConnection();
        await connection.OpenAsync(ct);
        var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM PushSubscriptions WHERE Endpoint = $endpoint AND BranchSlug = $branchSlug;";
        command.Parameters.AddWithValue("$endpoint", endpoint);
        command.Parameters.AddWithValue("$branchSlug", branchSlug);
        await command.ExecuteNonQueryAsync(ct);
    }

    public async Task<IReadOnlyList<PushSubscriptionRecord>> GetAllForBranchAsync(string branchSlug, CancellationToken ct)
    {
        using var connection = CreateConnection();
        await connection.OpenAsync(ct);
        var command = connection.CreateCommand();
        command.CommandText = "SELECT Id, Hn, QNumber, Endpoint, P256dh, Auth, LastLevel FROM PushSubscriptions WHERE BranchSlug = $branchSlug;";
        command.Parameters.AddWithValue("$branchSlug", branchSlug);

        var results = new List<PushSubscriptionRecord>();
        using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            results.Add(new PushSubscriptionRecord
            {
                Id = reader.GetInt64(0),
                Hn = reader.IsDBNull(1) ? null : reader.GetString(1),
                QNumber = reader.IsDBNull(2) ? null : reader.GetString(2),
                Endpoint = reader.GetString(3),
                P256dh = reader.GetString(4),
                Auth = reader.GetString(5),
                LastLevel = reader.GetInt32(6),
            });
        }

        return results;
    }

    public async Task UpdateLevelAsync(long id, int level, CancellationToken ct)
    {
        using var connection = CreateConnection();
        await connection.OpenAsync(ct);
        var command = connection.CreateCommand();
        command.CommandText = "UPDATE PushSubscriptions SET LastLevel = $level WHERE Id = $id;";
        command.Parameters.AddWithValue("$level", level);
        command.Parameters.AddWithValue("$id", id);
        await command.ExecuteNonQueryAsync(ct);
    }
}
