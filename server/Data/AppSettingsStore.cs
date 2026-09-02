using Microsoft.Data.Sqlite;
using QueOnline.Api.Models;

namespace QueOnline.Api.Data;

/// <summary>
/// Branding/behavior settings editable from the admin page — one row PER BRANCH (BranchSlug is
/// the primary key) in the same local SQLite file as push subscriptions (server/App_Data/), never
/// any hospital's Que_lcb database. Stays a Singleton with its own fixed SQLite connection (that
/// file's location doesn't vary by branch) — every method takes branchSlug explicitly instead,
/// supplied by the caller from IBranchContext.Current.Slug. A branch's row is created lazily on
/// first GetAsync (INSERT OR IGNORE) — so just adding a new entry to branches.json is enough for
/// that branch to work immediately, no manual migration step.
/// </summary>
public interface IAppSettingsStore
{
    Task InitializeAsync();
    Task<AppSettingsRecord> GetAsync(string branchSlug, CancellationToken ct);
    Task UpdateAsync(string branchSlug, UpdateSettingsRequest request, CancellationToken ct);
    Task UpdateLogoUrlAsync(string branchSlug, string logoUrl, CancellationToken ct);
    Task UpdateCustomSoundUrlAsync(string branchSlug, string soundUrl, CancellationToken ct);
}

public sealed class AppSettingsStore : IAppSettingsStore
{
    private readonly string _connectionString;

    public AppSettingsStore(IConfiguration configuration)
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

        // Pre-multi-branch dev databases hold a real single row (Id INTEGER PRIMARY KEY CHECK
        // (Id = 1)) with actual hand-customized branding — logo, custom sound, a hand-positioned
        // FieldLayoutJson, a chosen CardTemplate. That row must survive the schema change, not get
        // silently orphaned by a `CREATE TABLE IF NOT EXISTS` no-op against the old shape. Detect
        // the old shape and fold its one row into the "vibharam-dev" branch slug (branches.json's
        // first/real entry — this data is that hospital's actual settings) before the new
        // BranchSlug-keyed table is created.
        var oldShapeCommand = connection.CreateCommand();
        oldShapeCommand.CommandText = "SELECT COUNT(*) FROM pragma_table_info('AppSettings') WHERE name = 'Id';";
        var hasOldIdColumn = Convert.ToInt64(await oldShapeCommand.ExecuteScalarAsync()) > 0;

        if (hasOldIdColumn)
        {
            using var transaction = connection.BeginTransaction();

            var renameCommand = connection.CreateCommand();
            renameCommand.Transaction = transaction;
            renameCommand.CommandText = "ALTER TABLE AppSettings RENAME TO AppSettings_old;";
            await renameCommand.ExecuteNonQueryAsync();

            var createCommand = connection.CreateCommand();
            createCommand.Transaction = transaction;
            createCommand.CommandText = NewTableSql;
            await createCommand.ExecuteNonQueryAsync();

            var migrateCommand = connection.CreateCommand();
            migrateCommand.Transaction = transaction;
            migrateCommand.CommandText = """
                INSERT INTO AppSettings (
                    BranchSlug, HospitalName, Tagline, Greeting, ArrivedTitle, ArrivedBody,
                    PrimaryColor, SoundChoice, LogoUrl, FontFamily, FontSizeScale, CustomSoundUrl,
                    AccentColor, FieldLayoutJson, CardTemplate
                )
                SELECT
                    'vibharam-dev', HospitalName, Tagline, Greeting, ArrivedTitle, ArrivedBody,
                    PrimaryColor, SoundChoice, LogoUrl, FontFamily, FontSizeScale, CustomSoundUrl,
                    AccentColor, FieldLayoutJson, CardTemplate
                FROM AppSettings_old
                WHERE Id = 1;
                """;
            await migrateCommand.ExecuteNonQueryAsync();

            var dropCommand = connection.CreateCommand();
            dropCommand.Transaction = transaction;
            dropCommand.CommandText = "DROP TABLE AppSettings_old;";
            await dropCommand.ExecuteNonQueryAsync();

            transaction.Commit();
            return;
        }

        var command = connection.CreateCommand();
        command.CommandText = NewTableSql;
        await command.ExecuteNonQueryAsync();

        // 2026-09-02: CardBackgroundStyle added after this table already held real branch data —
        // ALTER TABLE ADD COLUMN (not a rebuild) is safe and sufficient here because it's a plain
        // new nullable-with-default column, unlike the earlier BranchSlug primary-key change.
        var hasBackgroundStyleColumn = connection.CreateCommand();
        hasBackgroundStyleColumn.CommandText = "SELECT COUNT(*) FROM pragma_table_info('AppSettings') WHERE name = 'CardBackgroundStyle';";
        var hasColumn = Convert.ToInt64(await hasBackgroundStyleColumn.ExecuteScalarAsync()) > 0;
        if (!hasColumn)
        {
            var addColumn = connection.CreateCommand();
            addColumn.CommandText = "ALTER TABLE AppSettings ADD COLUMN CardBackgroundStyle TEXT NOT NULL DEFAULT 'vivid';";
            await addColumn.ExecuteNonQueryAsync();
        }

        // 2026-09-02: same ADD COLUMN pattern for the new name/HN visibility toggles.
        var hasShowPatientNameColumn = connection.CreateCommand();
        hasShowPatientNameColumn.CommandText = "SELECT COUNT(*) FROM pragma_table_info('AppSettings') WHERE name = 'ShowPatientName';";
        if (Convert.ToInt64(await hasShowPatientNameColumn.ExecuteScalarAsync()) == 0)
        {
            var addColumn = connection.CreateCommand();
            addColumn.CommandText = "ALTER TABLE AppSettings ADD COLUMN ShowPatientName INTEGER NOT NULL DEFAULT 1;";
            await addColumn.ExecuteNonQueryAsync();
        }

        var hasShowHnColumn = connection.CreateCommand();
        hasShowHnColumn.CommandText = "SELECT COUNT(*) FROM pragma_table_info('AppSettings') WHERE name = 'ShowHn';";
        if (Convert.ToInt64(await hasShowHnColumn.ExecuteScalarAsync()) == 0)
        {
            var addColumn = connection.CreateCommand();
            addColumn.CommandText = "ALTER TABLE AppSettings ADD COLUMN ShowHn INTEGER NOT NULL DEFAULT 1;";
            await addColumn.ExecuteNonQueryAsync();
        }
    }

    private const string NewTableSql = """
        CREATE TABLE IF NOT EXISTS AppSettings (
            BranchSlug TEXT PRIMARY KEY,
            HospitalName TEXT NOT NULL,
            Tagline TEXT NOT NULL,
            Greeting TEXT NOT NULL,
            ArrivedTitle TEXT NOT NULL,
            ArrivedBody TEXT NOT NULL,
            PrimaryColor TEXT NOT NULL,
            SoundChoice TEXT NOT NULL,
            LogoUrl TEXT NULL,
            FontFamily TEXT NOT NULL DEFAULT 'Anuphan',
            FontSizeScale TEXT NOT NULL DEFAULT 'medium',
            CustomSoundUrl TEXT NULL,
            AccentColor TEXT NOT NULL DEFAULT '#e8720f',
            FieldLayoutJson TEXT NULL,
            CardTemplate TEXT NOT NULL DEFAULT 'custom',
            CardBackgroundStyle TEXT NOT NULL DEFAULT 'vivid',
            ShowPatientName INTEGER NOT NULL DEFAULT 1,
            ShowHn INTEGER NOT NULL DEFAULT 1
        );
        """;

    public async Task<AppSettingsRecord> GetAsync(string branchSlug, CancellationToken ct)
    {
        await EnsureBranchRowAsync(branchSlug, ct);

        using var connection = CreateConnection();
        await connection.OpenAsync(ct);
        var command = connection.CreateCommand();
        command.CommandText = """
            SELECT HospitalName, Tagline, Greeting, ArrivedTitle, ArrivedBody, PrimaryColor, SoundChoice, LogoUrl, FontFamily, FontSizeScale, CustomSoundUrl, AccentColor, FieldLayoutJson, CardTemplate, CardBackgroundStyle, ShowPatientName, ShowHn
            FROM AppSettings WHERE BranchSlug = $branchSlug;
            """;
        command.Parameters.AddWithValue("$branchSlug", branchSlug);

        using var reader = await command.ExecuteReaderAsync(ct);
        if (!await reader.ReadAsync(ct))
        {
            return new AppSettingsRecord();
        }

        return new AppSettingsRecord
        {
            HospitalName = reader.GetString(0),
            Tagline = reader.GetString(1),
            Greeting = reader.GetString(2),
            ArrivedTitle = reader.GetString(3),
            ArrivedBody = reader.GetString(4),
            PrimaryColor = reader.GetString(5),
            SoundChoice = reader.GetString(6),
            LogoUrl = reader.IsDBNull(7) ? null : reader.GetString(7),
            FontFamily = reader.GetString(8),
            FontSizeScale = reader.GetString(9),
            CustomSoundUrl = reader.IsDBNull(10) ? null : reader.GetString(10),
            AccentColor = reader.GetString(11),
            FieldLayoutJson = reader.IsDBNull(12) ? null : reader.GetString(12),
            CardTemplate = reader.GetString(13),
            CardBackgroundStyle = reader.GetString(14),
            ShowPatientName = reader.GetInt64(15) != 0,
            ShowHn = reader.GetInt64(16) != 0,
        };
    }

    /// <summary>Lazily creates a branch's settings row (all defaults) the first time it's touched
    /// — so adding a branch to branches.json is the only step needed to onboard it.</summary>
    private async Task EnsureBranchRowAsync(string branchSlug, CancellationToken ct)
    {
        var defaults = new AppSettingsRecord();
        using var connection = CreateConnection();
        await connection.OpenAsync(ct);
        var command = connection.CreateCommand();
        command.CommandText = """
            INSERT OR IGNORE INTO AppSettings (BranchSlug, HospitalName, Tagline, Greeting, ArrivedTitle, ArrivedBody, PrimaryColor, AccentColor, SoundChoice, LogoUrl, FontFamily, FontSizeScale, CustomSoundUrl, CardTemplate, CardBackgroundStyle, ShowPatientName, ShowHn)
            VALUES ($branchSlug, $hospitalName, $tagline, $greeting, $arrivedTitle, $arrivedBody, $primaryColor, $accentColor, $soundChoice, NULL, $fontFamily, $fontSizeScale, NULL, $cardTemplate, $cardBackgroundStyle, $showPatientName, $showHn);
            """;
        command.Parameters.AddWithValue("$branchSlug", branchSlug);
        command.Parameters.AddWithValue("$hospitalName", defaults.HospitalName);
        command.Parameters.AddWithValue("$tagline", defaults.Tagline);
        command.Parameters.AddWithValue("$greeting", defaults.Greeting);
        command.Parameters.AddWithValue("$arrivedTitle", defaults.ArrivedTitle);
        command.Parameters.AddWithValue("$arrivedBody", defaults.ArrivedBody);
        command.Parameters.AddWithValue("$primaryColor", defaults.PrimaryColor);
        command.Parameters.AddWithValue("$accentColor", defaults.AccentColor);
        command.Parameters.AddWithValue("$soundChoice", defaults.SoundChoice);
        command.Parameters.AddWithValue("$fontFamily", defaults.FontFamily);
        command.Parameters.AddWithValue("$fontSizeScale", defaults.FontSizeScale);
        command.Parameters.AddWithValue("$cardTemplate", defaults.CardTemplate);
        command.Parameters.AddWithValue("$cardBackgroundStyle", defaults.CardBackgroundStyle);
        command.Parameters.AddWithValue("$showPatientName", defaults.ShowPatientName ? 1 : 0);
        command.Parameters.AddWithValue("$showHn", defaults.ShowHn ? 1 : 0);
        await command.ExecuteNonQueryAsync(ct);
    }

    public async Task UpdateAsync(string branchSlug, UpdateSettingsRequest request, CancellationToken ct)
    {
        var current = await GetAsync(branchSlug, ct);

        using var connection = CreateConnection();
        await connection.OpenAsync(ct);
        var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE AppSettings SET
                HospitalName = $hospitalName,
                Tagline = $tagline,
                Greeting = $greeting,
                ArrivedTitle = $arrivedTitle,
                ArrivedBody = $arrivedBody,
                PrimaryColor = $primaryColor,
                AccentColor = $accentColor,
                SoundChoice = $soundChoice,
                FontFamily = $fontFamily,
                FontSizeScale = $fontSizeScale,
                FieldLayoutJson = $fieldLayoutJson,
                CardTemplate = $cardTemplate,
                CardBackgroundStyle = $cardBackgroundStyle,
                ShowPatientName = $showPatientName,
                ShowHn = $showHn
            WHERE BranchSlug = $branchSlug;
            """;
        command.Parameters.AddWithValue("$branchSlug", branchSlug);
        command.Parameters.AddWithValue("$hospitalName", request.HospitalName ?? current.HospitalName);
        command.Parameters.AddWithValue("$tagline", request.Tagline ?? current.Tagline);
        command.Parameters.AddWithValue("$greeting", request.Greeting ?? current.Greeting);
        command.Parameters.AddWithValue("$arrivedTitle", request.ArrivedTitle ?? current.ArrivedTitle);
        command.Parameters.AddWithValue("$arrivedBody", request.ArrivedBody ?? current.ArrivedBody);
        command.Parameters.AddWithValue("$primaryColor", request.PrimaryColor ?? current.PrimaryColor);
        command.Parameters.AddWithValue("$accentColor", request.AccentColor ?? current.AccentColor);
        command.Parameters.AddWithValue("$soundChoice", request.SoundChoice ?? current.SoundChoice);
        command.Parameters.AddWithValue("$fontFamily", request.FontFamily ?? current.FontFamily);
        command.Parameters.AddWithValue("$fontSizeScale", request.FontSizeScale ?? current.FontSizeScale);
        command.Parameters.AddWithValue("$fieldLayoutJson", (object?)(request.FieldLayoutJson ?? current.FieldLayoutJson) ?? DBNull.Value);
        command.Parameters.AddWithValue("$cardTemplate", request.CardTemplate ?? current.CardTemplate);
        command.Parameters.AddWithValue("$cardBackgroundStyle", request.CardBackgroundStyle ?? current.CardBackgroundStyle);
        command.Parameters.AddWithValue("$showPatientName", (request.ShowPatientName ?? current.ShowPatientName) ? 1 : 0);
        command.Parameters.AddWithValue("$showHn", (request.ShowHn ?? current.ShowHn) ? 1 : 0);
        await command.ExecuteNonQueryAsync(ct);
    }

    public async Task UpdateLogoUrlAsync(string branchSlug, string logoUrl, CancellationToken ct)
    {
        await EnsureBranchRowAsync(branchSlug, ct);
        using var connection = CreateConnection();
        await connection.OpenAsync(ct);
        var command = connection.CreateCommand();
        command.CommandText = "UPDATE AppSettings SET LogoUrl = $logoUrl WHERE BranchSlug = $branchSlug;";
        command.Parameters.AddWithValue("$logoUrl", logoUrl);
        command.Parameters.AddWithValue("$branchSlug", branchSlug);
        await command.ExecuteNonQueryAsync(ct);
    }

    public async Task UpdateCustomSoundUrlAsync(string branchSlug, string soundUrl, CancellationToken ct)
    {
        await EnsureBranchRowAsync(branchSlug, ct);
        using var connection = CreateConnection();
        await connection.OpenAsync(ct);
        var command = connection.CreateCommand();
        command.CommandText = "UPDATE AppSettings SET CustomSoundUrl = $soundUrl WHERE BranchSlug = $branchSlug;";
        command.Parameters.AddWithValue("$soundUrl", soundUrl);
        command.Parameters.AddWithValue("$branchSlug", branchSlug);
        await command.ExecuteNonQueryAsync(ct);
    }
}
