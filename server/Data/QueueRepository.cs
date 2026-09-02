using Dapper;
using Microsoft.Data.SqlClient;
using QueOnline.Api.Models;

namespace QueOnline.Api.Data;

public interface IQueueRepository
{
    Task<IReadOnlyList<DepartmentOption>> GetDepartmentsAsync(CancellationToken ct);
    Task<IReadOnlyList<BoardQueueItem>> GetBoardAsync(string? groupCode, CancellationToken ct);
    Task<QueueLookupResult?> LookupAsync(string? hn, string? queueNumber, CancellationToken ct);
}

/// <summary>
/// Sole data source for the app (2026-08-31) — QueReg/registration-counter support was removed
/// at the hospital's request; everything now reads Queue_q only.
/// </summary>
public sealed class QueueRepository : IQueueRepository
{
    private const string SelectColumns = """
        q.qid            AS Qid,
        q.qnumber        AS QNumber,
        q.qdep           AS QDep,
        q.qstatus        AS QStatus,
        q.qocmfullname   AS QOcmFullName,
        q.qocmchatnum    AS QOcmChatNum,
        q.qocmVst        AS QOcmVst,
        q.qcounter       AS QCounter,
        q.qdatetime      AS QDateTime,
        q.qtimecall      AS QTimeCall,
        q.qtimecalllab   AS QTimeCallLab,
        q.qroomlab       AS QRoomLab,
        q.qtimecallfin   AS QTimeCallFin,
        q.qroomfin       AS QRoomFin,
        q.qtimecallpha   AS QTimeCallPha,
        q.qroompha       AS QRoomPha,
        g.grucode        AS GruCode,
        g.grugroup       AS GruGroup,
        d.depname        AS DepName
        """;

    // MasterGroup.depid is a short code (e.g. "A", "F", "VP") that prefixes Queue_q.qdep
    // (e.g. "A01"), which is why the join has to compare against LEFT(q.qdep, LEN(g.depid))
    // rather than an exact match — mirrors StGetQueData_Display / GetQuecall.
    private const string FromJoins = """
        FROM Queue_q q WITH (NOLOCK)
        LEFT JOIN MasterGroup g WITH (NOLOCK) ON g.depid = LEFT(q.qdep, LEN(g.depid))
        LEFT JOIN MasterDep d WITH (NOLOCK) ON d.depdtlcod = q.qnrsstn
        """;

    private readonly IBranchContext _branchContext;
    private readonly ILogger<QueueRepository> _logger;

    public QueueRepository(IBranchContext branchContext, ILogger<QueueRepository> logger)
    {
        _branchContext = branchContext;
        _logger = logger;
    }

    // Read lazily (not cached at construction) so this never depends on DI construction order
    // relative to the branch-resolution middleware that sets IBranchContext.Current.
    private SqlConnection CreateConnection() => new(_branchContext.Current.ConnectionString);

    public async Task<IReadOnlyList<DepartmentOption>> GetDepartmentsAsync(CancellationToken ct)
    {
        const string sql = """
            SELECT grucode AS Code, grugroup AS Name
            FROM MasterGroup WITH (NOLOCK)
            WHERE grucode IS NOT NULL AND grugroup IS NOT NULL
            ORDER BY priority DESC, grugroup ASC;
            """;

        using var connection = CreateConnection();
        var command = new CommandDefinition(sql, cancellationToken: ct);
        var rows = await connection.QueryAsync<DepartmentOption>(command);
        return rows.ToList();
    }

    public async Task<IReadOnlyList<BoardQueueItem>> GetBoardAsync(string? groupCode, CancellationToken ct)
    {
        var sql = $"""
            SELECT {SelectColumns}
            {FromJoins}
            WHERE q.qdatetime LIKE @TodayPrefix + '%'
              AND (@GroupCode IS NULL OR g.grucode = @GroupCode OR g.targetgroup = @GroupCode)
            ORDER BY
              CASE WHEN q.qtimecall IS NULL OR q.qtimecall = '' THEN 0 ELSE 1 END, -- not-yet-called first
              q.qdatetime ASC;
            """;

        using var connection = CreateConnection();
        var command = new CommandDefinition(
            sql,
            new { TodayPrefix = QueueTimeFormat.TodayPrefix(), GroupCode = string.IsNullOrWhiteSpace(groupCode) ? null : groupCode },
            cancellationToken: ct);
        var rows = await connection.QueryAsync<QueueRow>(command);
        return rows.Select(MapBoardItem).ToList();
    }

    /// <summary>hn matches qocmchatnum (the hospital's HN field on this table — NOT qocmnum, which is a separate internal id).</summary>
    public async Task<QueueLookupResult?> LookupAsync(string? hn, string? queueNumber, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(hn) && string.IsNullOrWhiteSpace(queueNumber))
        {
            return null;
        }

        var sql = $"""
            SELECT TOP (1) {SelectColumns}
            {FromJoins}
            WHERE q.qdatetime LIKE @TodayPrefix + '%'
              AND (
                    (@Hn IS NOT NULL AND q.qocmchatnum = @Hn)
                 OR (@QueueNumber IS NOT NULL AND q.qnumber = @QueueNumber)
              )
            ORDER BY q.qid DESC;
            """;

        using var connection = CreateConnection();
        var command = new CommandDefinition(
            sql,
            new
            {
                TodayPrefix = QueueTimeFormat.TodayPrefix(),
                Hn = string.IsNullOrWhiteSpace(hn) ? null : hn.Trim(),
                QueueNumber = string.IsNullOrWhiteSpace(queueNumber) ? null : queueNumber.Trim(),
            },
            cancellationToken: ct);
        var row = await connection.QuerySingleOrDefaultAsync<QueueRow>(command);
        if (row is null)
        {
            return null;
        }

        var aheadCount = await CountAheadAsync(connection, row, ct);
        var (currentServing, estimatedWait, sampleSize) = await ComputeWaitStatsAsync(connection, row, aheadCount, ct);
        var timeline = await BuildTimelineWithStageStatsAsync(connection, row, ct);
        return MapLookupResult(row, aheadCount, currentServing, estimatedWait, sampleSize, timeline);
    }

    // (StepKey, its own timestamp column, the columns of every stage AFTER it) — used both to
    // decide which step in the timeline gets an ahead-count/wait estimate attached, and to build
    // the "not yet resolved" SQL condition for each (see CountStageAheadAsync doc comment).
    private static readonly (string StepKey, string TimeColumn, string[] LaterColumns)[] StageEstimateSpecs =
    [
        ("lab", "qtimecalllab", ["qtimecallfin", "qtimecallpha"]),
        ("fin", "qtimecallfin", ["qtimecallpha"]),
        ("pha", "qtimecallpha", []),
    ];

    /// <summary>Builds the base timeline, then — for whichever of lab/fin/pha is this visit's actual
    /// current step (not done, not skipped, already past the initial call) — attaches an ahead-count
    /// and wait estimate to that one step. See the AheadCount/EstimatedWaitMinutes doc comment on
    /// QueueTimelineStep for why this is an approximation.</summary>
    private async Task<List<QueueTimelineStep>> BuildTimelineWithStageStatsAsync(SqlConnection connection, QueueRow row, CancellationToken ct)
    {
        var timeline = BuildTimeline(row);
        if (string.IsNullOrWhiteSpace(row.GruCode) || !QueueTimeFormat.HasValue(row.QTimeCall))
        {
            return timeline;
        }

        for (var i = 0; i < timeline.Count; i++)
        {
            var step = timeline[i];
            if (step.Done || step.Skipped) continue;

            var spec = StageEstimateSpecs.FirstOrDefault(s => s.StepKey == step.Key);
            if (spec.StepKey is null) continue; // register/call aren't estimated here — call already has its own top-level stats

            var ahead = await CountStageAheadAsync(connection, row, spec.TimeColumn, spec.LaterColumns, ct);
            var (_, estimatedWait, _) = await ComputeStageWaitStatsAsync(connection, row, spec.TimeColumn, ahead, ct);
            timeline[i] = step with { AheadCount = ahead, EstimatedWaitMinutes = estimatedWait };
            break; // only the current step gets stats — everything after it is still ahead of not-yet-known info
        }

        return timeline;
    }

    /// <summary>"Ahead of me for stage X" = visits already called into the exam room, in the same
    /// group, called before me, that haven't resolved stage X yet — resolved meaning either X's own
    /// timestamp is set, or ANY later stage's is (which means they skipped X and moved on). This can
    /// overcount: someone in this set might turn out to skip X too and never show up there, since
    /// Queue_q has no field saying in advance whether a visit will need stage X at all (confirmed
    /// against real data 2026-09-01 — qroomlab/qtimecalllab etc. only ever appear together, set at
    /// the moment the call happens). Confirmed acceptable with the hospital as a best-effort
    /// approximation rather than an exact count.</summary>
    private async Task<int> CountStageAheadAsync(SqlConnection connection, QueueRow row, string timeColumn, string[] laterColumns, CancellationToken ct)
    {
        var laterConditions = string.Concat(laterColumns.Select(c => $" AND (q2.{c} IS NULL OR q2.{c} = '')"));
        var sql = $"""
            SELECT COUNT(*)
            FROM Queue_q q2 WITH (NOLOCK)
            LEFT JOIN MasterGroup g2 WITH (NOLOCK) ON g2.depid = LEFT(q2.qdep, LEN(g2.depid))
            WHERE q2.qdatetime LIKE @TodayPrefix + '%'
              AND (g2.grucode = @GroupCode OR g2.targetgroup = @GroupCode)
              AND q2.qtimecall IS NOT NULL AND q2.qtimecall <> ''
              AND (q2.{timeColumn} IS NULL OR q2.{timeColumn} = ''){laterConditions}
              AND q2.qtimecall < @MyCallTime;
            """;

        var command = new CommandDefinition(
            sql,
            new { TodayPrefix = QueueTimeFormat.TodayPrefix(), GroupCode = row.GruCode, MyCallTime = row.QTimeCall },
            cancellationToken: ct);
        return await connection.ExecuteScalarAsync<int>(command);
    }

    /// <summary>Same average-gap-between-calls estimate as ComputeWaitStatsAsync, just sourced from
    /// stage X's own call timestamps today instead of qtimecall.</summary>
    private async Task<(string? CurrentServing, int? EstimatedWaitMinutes, int SampleSize)> ComputeStageWaitStatsAsync(
        SqlConnection connection, QueueRow row, string timeColumn, int aheadCount, CancellationToken ct)
    {
        var sql = $"""
            SELECT q2.qnumber AS QNumber, q2.{timeColumn} AS QTimeCall
            FROM Queue_q q2 WITH (NOLOCK)
            LEFT JOIN MasterGroup g2 WITH (NOLOCK) ON g2.depid = LEFT(q2.qdep, LEN(g2.depid))
            WHERE q2.qdatetime LIKE @TodayPrefix + '%'
              AND q2.{timeColumn} IS NOT NULL AND q2.{timeColumn} <> ''
              AND (g2.grucode = @GroupCode OR g2.targetgroup = @GroupCode);
            """;

        var command = new CommandDefinition(
            sql,
            new { TodayPrefix = QueueTimeFormat.TodayPrefix(), GroupCode = row.GruCode },
            cancellationToken: ct);
        var resolvedToday = (await connection.QueryAsync<CalledRow>(command)).ToList();
        return QueueWaitEstimate.Compute(resolvedToday, aheadCount);
    }

    private async Task<(string? CurrentServing, int? EstimatedWaitMinutes, int SampleSize)> ComputeWaitStatsAsync(
        SqlConnection connection, QueueRow row, int aheadCount, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(row.GruCode))
        {
            return (null, null, 0);
        }

        const string sql = """
            SELECT q2.qnumber AS QNumber, q2.qtimecall AS QTimeCall
            FROM Queue_q q2 WITH (NOLOCK)
            LEFT JOIN MasterGroup g2 WITH (NOLOCK) ON g2.depid = LEFT(q2.qdep, LEN(g2.depid))
            WHERE q2.qdatetime LIKE @TodayPrefix + '%'
              AND q2.qtimecall IS NOT NULL AND q2.qtimecall <> ''
              AND (g2.grucode = @GroupCode OR g2.targetgroup = @GroupCode);
            """;

        var command = new CommandDefinition(
            sql,
            new { TodayPrefix = QueueTimeFormat.TodayPrefix(), GroupCode = row.GruCode },
            cancellationToken: ct);
        var calledToday = (await connection.QueryAsync<CalledRow>(command)).ToList();
        return QueueWaitEstimate.Compute(calledToday, aheadCount);
    }

    private async Task<int> CountAheadAsync(SqlConnection connection, QueueRow row, CancellationToken ct)
    {
        // Only meaningful while the patient hasn't been called yet; once called there is no "ahead" count.
        if (QueueTimeFormat.HasValue(row.QTimeCall) || string.IsNullOrWhiteSpace(row.QDep))
        {
            return 0;
        }

        const string sql = """
            SELECT COUNT(*)
            FROM Queue_q q2 WITH (NOLOCK)
            WHERE q2.qdep = @QDep
              AND q2.qdatetime LIKE @TodayPrefix + '%'
              AND (q2.qtimecall IS NULL OR q2.qtimecall = '')
              AND q2.qdatetime < @MyRegister;
            """;

        var command = new CommandDefinition(
            sql,
            new { row.QDep, TodayPrefix = QueueTimeFormat.TodayPrefix(), MyRegister = row.QDateTime },
            cancellationToken: ct);
        return await connection.ExecuteScalarAsync<int>(command);
    }

    private static BoardQueueItem MapBoardItem(QueueRow row) => new(
        row.Qid,
        row.QNumber,
        row.QOcmFullName,
        row.DepName,
        row.GruCode,
        row.GruGroup,
        row.QCounter,
        row.QStatus ?? string.Empty,
        QueueStatusLabel.ToLabel(row.QStatus),
        QueueTimeFormat.Parse(row.QDateTime),
        QueueTimeFormat.Parse(row.QTimeCall)
    );

    private static QueueLookupResult MapLookupResult(
        QueueRow row, int aheadCount, string? currentServing, int? estimatedWaitMinutes, int sampleSize, List<QueueTimelineStep> timeline) => new(
        row.Qid,
        row.QNumber,
        row.QOcmFullName,
        row.QOcmChatNum,
        row.QOcmVst,
        row.DepName,
        row.GruCode,
        row.GruGroup,
        row.QStatus ?? string.Empty,
        QueueStatusLabel.ToLabel(row.QStatus),
        aheadCount,
        timeline,
        currentServing,
        estimatedWaitMinutes,
        sampleSize
    );

    // Fixed 5-stage pipeline, always shown in full regardless of whether a given visit actually
    // goes through lab/finance/pharmacy today — the hospital wants the whole flow visible up
    // front, not stages that pop in/out depending on data.
    //
    // Not every visit touches every stage (e.g. no lab test ordered) — checked directly against
    // real rows in Que_lcb (2026-09-01): qroomlab/qtimecalllab (and the fin/pha equivalents) only
    // ever appear together, set at the moment that stage's call actually happens, so there's no
    // advance signal distinguishing "hasn't happened yet" from "was never going to happen." The
    // only thing we CAN tell after the fact is that a stage was skipped: it never got a timestamp,
    // but a LATER stage did — the visit moved on without it. A step already flagged Skipped is
    // never picked as the "current" step client-side (see currentStepKey in QueueStatusView.vue).
    private static List<QueueTimelineStep> BuildTimeline(QueueRow row)
    {
        var raw = new (string Key, string Label, string? Location, string? RawAt)[]
        {
            ("register", "ลงทะเบียน", null, row.QDateTime),
            ("call", "เรียกเข้าห้องตรวจ", row.QCounter, row.QTimeCall),
            ("lab", "ตรวจแล็ป", row.QRoomLab, row.QTimeCallLab),
            ("fin", "การเงิน", row.QRoomFin, row.QTimeCallFin),
            ("pha", "ห้องยา", row.QRoomPha, row.QTimeCallPha),
        };

        var done = raw.Select(r => QueueTimeFormat.HasValue(r.RawAt)).ToArray();
        var anyLaterDone = new bool[raw.Length];
        var sawDoneAfter = false;
        for (var i = raw.Length - 1; i >= 0; i--)
        {
            anyLaterDone[i] = sawDoneAfter;
            if (done[i]) sawDoneAfter = true;
        }

        return raw
            .Select((r, i) => new QueueTimelineStep(
                r.Key, r.Label, r.Location, QueueTimeFormat.Parse(r.RawAt), done[i], Skipped: !done[i] && anyLaterDone[i]))
            .ToList();
    }
}
