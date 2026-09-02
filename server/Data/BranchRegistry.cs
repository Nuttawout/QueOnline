using Microsoft.AspNetCore.DataProtection;
using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;
using QueOnline.Api.Models;

namespace QueOnline.Api.Data;

/// <summary>The only place branch DB credentials/admin passwords are ever read/written —
/// everything else (controllers, QueueRepository, AdminController) goes through this, never touches
/// the underlying SQLite file or IConfiguration directly, so there is exactly one path a secret can
/// leak through to review. Backed by a dedicated local SQLite file (server/App_Data/branches.db,
/// separate from push_subscriptions.db on purpose — this file holds real SQL Server passwords and
/// branch admin passwords, branding assets don't need the same blast-radius). DbPassword/
/// AdminPassword are encrypted at rest via ASP.NET Core's Data Protection API (keys persisted next
/// to the DB file, DPAPI-protected on Windows) — plaintext only ever exists in memory for an
/// authenticated super-admin request.
///
/// Editable at runtime through SuperAdminController, gated by its own master password (SuperAdmin:
/// Password in config) — a separate credential from any branch's own admin password, since this
/// surface can see/edit every branch's secrets at once.
///
/// server/branches.json (the old config-file source of truth, pre-2026-09-01) is still read once,
/// ONLY as a one-time seed if this table is empty on first startup — after that it's never read
/// again; this store is the sole source of truth going forward.</summary>
public interface IBranchRegistry
{
    Task InitializeAsync(CancellationToken ct = default);
    Task<Branch?> FindBySlugAsync(string slug, CancellationToken ct);
    Task<IReadOnlyList<Branch>> GetAllAsync(CancellationToken ct);
    Task UpsertAsync(Branch branch, CancellationToken ct);
    Task<bool> DeleteAsync(string slug, CancellationToken ct);
}

public sealed class BranchRegistry : IBranchRegistry
{
    private readonly string _connectionString;
    private readonly IConfiguration _configuration;
    private readonly IDataProtector _protector;

    public BranchRegistry(IConfiguration configuration, IDataProtectionProvider dataProtectionProvider)
    {
        _configuration = configuration;
        var dbPath = configuration["BranchDbPath"] ?? "App_Data/branches.db";
        var fullPath = Path.IsPathRooted(dbPath) ? dbPath : Path.Combine(AppContext.BaseDirectory, "..", "..", "..", dbPath);
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(fullPath))!);
        _connectionString = $"Data Source={Path.GetFullPath(fullPath)}";
        _protector = dataProtectionProvider.CreateProtector("QueOnline.BranchSecrets.v1");
    }

    private SqliteConnection CreateConnection() => new(_connectionString);

    private const string NewTableSql = """
        CREATE TABLE IF NOT EXISTS Branches (
            Slug TEXT PRIMARY KEY,
            DisplayName TEXT NOT NULL,
            ServerName TEXT NOT NULL,
            DatabaseName TEXT NOT NULL,
            DbLogin TEXT NOT NULL,
            DbPasswordProtected TEXT NOT NULL,
            EncryptTransfer INTEGER NOT NULL DEFAULT 1,
            AdminPasswordProtected TEXT NOT NULL,
            CreatedAt TEXT NOT NULL,
            UpdatedAt TEXT NOT NULL
        );
        """;

    public async Task InitializeAsync(CancellationToken ct = default)
    {
        using var connection = CreateConnection();
        await connection.OpenAsync(ct);

        // 2026-09-02: schema changed from one free-text ConnectionString column to structured
        // fields (ServerName/DatabaseName/DbLogin/DbPassword/EncryptTransfer) matching how branch
        // IT hands these over. Detect the old shape and migrate any existing rows in place before
        // the new-shape CREATE TABLE below (a no-op once this has run).
        var oldColumnCheck = connection.CreateCommand();
        oldColumnCheck.CommandText = "SELECT COUNT(*) FROM pragma_table_info('Branches') WHERE name = 'ConnectionStringProtected';";
        var hasOldSchema = Convert.ToInt64(await oldColumnCheck.ExecuteScalarAsync(ct)) > 0;
        if (hasOldSchema)
        {
            await MigrateFromConnectionStringSchemaAsync(connection, ct);
        }

        var create = connection.CreateCommand();
        create.CommandText = NewTableSql;
        await create.ExecuteNonQueryAsync(ct);

        var countCommand = connection.CreateCommand();
        countCommand.CommandText = "SELECT COUNT(*) FROM Branches;";
        var existingCount = Convert.ToInt64(await countCommand.ExecuteScalarAsync(ct));
        if (existingCount > 0)
        {
            return;
        }

        // One-time bootstrap from the old branches.json config file, only when the table has never
        // been populated — lets a dev machine that still has branches.json on disk keep its existing
        // branches instead of starting from zero. A fresh install with no branches.json just starts
        // with an empty table (add branches via the super-admin UI).
        var seedBranches = _configuration.GetSection("Branches").Get<List<LegacyBranchJson>>() ?? [];
        foreach (var legacy in seedBranches)
        {
            await UpsertInternalAsync(connection, ParseLegacyConnectionString(legacy), ct);
        }
    }

    /// <summary>branches.json's old shape — a single ADO.NET connection string, not structured
    /// fields. Only used to parse the one-time seed above; never used elsewhere.</summary>
    private sealed record LegacyBranchJson(string Slug, string DisplayName, string ConnectionString, string AdminPassword);

    private static Branch ParseLegacyConnectionString(LegacyBranchJson legacy)
    {
        var csBuilder = new SqlConnectionStringBuilder(legacy.ConnectionString);
        return new Branch(legacy.Slug, legacy.DisplayName, csBuilder.DataSource, csBuilder.InitialCatalog, csBuilder.UserID, csBuilder.Password, csBuilder.Encrypt, legacy.AdminPassword);
    }

    private async Task MigrateFromConnectionStringSchemaAsync(SqliteConnection connection, CancellationToken ct)
    {
        using var transaction = connection.BeginTransaction();

        var rename = connection.CreateCommand();
        rename.Transaction = transaction;
        rename.CommandText = "ALTER TABLE Branches RENAME TO Branches_old;";
        await rename.ExecuteNonQueryAsync(ct);

        var create = connection.CreateCommand();
        create.Transaction = transaction;
        create.CommandText = NewTableSql;
        await create.ExecuteNonQueryAsync(ct);

        var selectOld = connection.CreateCommand();
        selectOld.Transaction = transaction;
        selectOld.CommandText = "SELECT Slug, DisplayName, ConnectionStringProtected, AdminPasswordProtected, CreatedAt FROM Branches_old;";
        var oldRows = new List<(string Slug, string DisplayName, string ConnectionStringProtected, string AdminPasswordProtected, string CreatedAt)>();
        using (var reader = await selectOld.ExecuteReaderAsync(ct))
        {
            while (await reader.ReadAsync(ct))
            {
                oldRows.Add((reader.GetString(0), reader.GetString(1), reader.GetString(2), reader.GetString(3), reader.GetString(4)));
            }
        }

        foreach (var old in oldRows)
        {
            var csBuilder = new SqlConnectionStringBuilder(_protector.Unprotect(old.ConnectionStringProtected));
            var insert = connection.CreateCommand();
            insert.Transaction = transaction;
            insert.CommandText = """
                INSERT INTO Branches (Slug, DisplayName, ServerName, DatabaseName, DbLogin, DbPasswordProtected, EncryptTransfer, AdminPasswordProtected, CreatedAt, UpdatedAt)
                VALUES ($slug, $displayName, $serverName, $databaseName, $dbLogin, $dbPassword, $encrypt, $adminPassword, $createdAt, $updatedAt);
                """;
            insert.Parameters.AddWithValue("$slug", old.Slug);
            insert.Parameters.AddWithValue("$displayName", old.DisplayName);
            insert.Parameters.AddWithValue("$serverName", csBuilder.DataSource);
            insert.Parameters.AddWithValue("$databaseName", csBuilder.InitialCatalog);
            insert.Parameters.AddWithValue("$dbLogin", csBuilder.UserID);
            insert.Parameters.AddWithValue("$dbPassword", _protector.Protect(csBuilder.Password));
            insert.Parameters.AddWithValue("$encrypt", csBuilder.Encrypt ? 1 : 0);
            insert.Parameters.AddWithValue("$adminPassword", old.AdminPasswordProtected); // already protected — carried over as-is
            insert.Parameters.AddWithValue("$createdAt", old.CreatedAt);
            insert.Parameters.AddWithValue("$updatedAt", DateTime.UtcNow.ToString("O"));
            await insert.ExecuteNonQueryAsync(ct);
        }

        var drop = connection.CreateCommand();
        drop.Transaction = transaction;
        drop.CommandText = "DROP TABLE Branches_old;";
        await drop.ExecuteNonQueryAsync(ct);

        transaction.Commit();
    }

    public async Task<Branch?> FindBySlugAsync(string slug, CancellationToken ct)
    {
        using var connection = CreateConnection();
        await connection.OpenAsync(ct);
        var command = connection.CreateCommand();
        command.CommandText = """
            SELECT Slug, DisplayName, ServerName, DatabaseName, DbLogin, DbPasswordProtected, EncryptTransfer, AdminPasswordProtected
            FROM Branches WHERE Slug = $slug COLLATE NOCASE;
            """;
        command.Parameters.AddWithValue("$slug", slug);
        using var reader = await command.ExecuteReaderAsync(ct);
        return await reader.ReadAsync(ct) ? ReadBranch(reader) : null;
    }

    public async Task<IReadOnlyList<Branch>> GetAllAsync(CancellationToken ct)
    {
        using var connection = CreateConnection();
        await connection.OpenAsync(ct);
        var command = connection.CreateCommand();
        command.CommandText = """
            SELECT Slug, DisplayName, ServerName, DatabaseName, DbLogin, DbPasswordProtected, EncryptTransfer, AdminPasswordProtected
            FROM Branches ORDER BY DisplayName COLLATE NOCASE;
            """;
        var results = new List<Branch>();
        using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            results.Add(ReadBranch(reader));
        }
        return results;
    }

    private Branch ReadBranch(SqliteDataReader reader) => new(
        reader.GetString(0),
        reader.GetString(1),
        reader.GetString(2),
        reader.GetString(3),
        reader.GetString(4),
        _protector.Unprotect(reader.GetString(5)),
        reader.GetInt64(6) != 0,
        _protector.Unprotect(reader.GetString(7)));

    public async Task UpsertAsync(Branch branch, CancellationToken ct)
    {
        using var connection = CreateConnection();
        await connection.OpenAsync(ct);
        await UpsertInternalAsync(connection, branch, ct);
    }

    private async Task UpsertInternalAsync(SqliteConnection connection, Branch branch, CancellationToken ct)
    {
        var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO Branches (Slug, DisplayName, ServerName, DatabaseName, DbLogin, DbPasswordProtected, EncryptTransfer, AdminPasswordProtected, CreatedAt, UpdatedAt)
            VALUES ($slug, $displayName, $serverName, $databaseName, $dbLogin, $dbPassword, $encrypt, $adminPassword, $now, $now)
            ON CONFLICT(Slug) DO UPDATE SET
                DisplayName = excluded.DisplayName,
                ServerName = excluded.ServerName,
                DatabaseName = excluded.DatabaseName,
                DbLogin = excluded.DbLogin,
                DbPasswordProtected = excluded.DbPasswordProtected,
                EncryptTransfer = excluded.EncryptTransfer,
                AdminPasswordProtected = excluded.AdminPasswordProtected,
                UpdatedAt = excluded.UpdatedAt;
            """;
        command.Parameters.AddWithValue("$slug", branch.Slug);
        command.Parameters.AddWithValue("$displayName", branch.DisplayName);
        command.Parameters.AddWithValue("$serverName", branch.ServerName);
        command.Parameters.AddWithValue("$databaseName", branch.DatabaseName);
        command.Parameters.AddWithValue("$dbLogin", branch.DbLogin);
        command.Parameters.AddWithValue("$dbPassword", _protector.Protect(branch.DbPassword));
        command.Parameters.AddWithValue("$encrypt", branch.EncryptTransfer ? 1 : 0);
        command.Parameters.AddWithValue("$adminPassword", _protector.Protect(branch.AdminPassword));
        command.Parameters.AddWithValue("$now", DateTime.UtcNow.ToString("O"));
        await command.ExecuteNonQueryAsync(ct);
    }

    public async Task<bool> DeleteAsync(string slug, CancellationToken ct)
    {
        using var connection = CreateConnection();
        await connection.OpenAsync(ct);
        var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM Branches WHERE Slug = $slug COLLATE NOCASE;";
        command.Parameters.AddWithValue("$slug", slug);
        var rows = await command.ExecuteNonQueryAsync(ct);
        return rows > 0;
    }
}
