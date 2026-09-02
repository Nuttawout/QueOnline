using System.Globalization;

namespace QueOnline.Api.Models;

/// <summary>
/// Queue_q stores every timestamp column as free-text varchar in "yyyyMMddHHmmss" format
/// (confirmed from the existing StGetQueData_Display / GetQuecallfin stored procedures,
/// which slice it with SUBSTRING at fixed positions). There is no CHECK constraint enforcing
/// this, so parsing is defensive: an unparseable value just comes back as null rather than throwing.
/// </summary>
public static class QueueTimeFormat
{
    private const string Format = "yyyyMMddHHmmss";

    public static DateTime? Parse(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        var trimmed = raw.Trim();
        if (DateTime.TryParseExact(trimmed, Format, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
        {
            return parsed;
        }

        return null;
    }

    /// <summary>Today's date as the "yyyyMMdd" prefix Queue_q rows are filtered by (server local time).</summary>
    public static string TodayPrefix() => DateTime.Now.ToString("yyyyMMdd", CultureInfo.InvariantCulture);

    /// <summary>
    /// "Has this stage actually happened?" — data entered directly into Queue_q/QueReg (rather
    /// than through the original kiosk/staff app) can leave timestamp columns as an empty string
    /// instead of a true SQL NULL. A plain `column is not null` check in C#, or `IS NOT NULL` in
    /// SQL, treats "" as present and wrongly marks a stage "done" — always go through this helper
    /// (or its SQL equivalent, `col IS NOT NULL AND col <> ''`) instead of a raw null check.
    /// </summary>
    public static bool HasValue(string? raw) => !string.IsNullOrWhiteSpace(raw);
}

/// <summary>
/// qstatus label map — confirmed directly by the hospital (2026-08-31): S = new/waiting,
/// L = at lab, F = at finance/billing, P = at pharmacy. Replaces an earlier guess inferred from
/// legacy stored procedures (Y/recall/Frecall/Precall) that turned out not to match this table's
/// real usage. Unknown codes still fall back to showing the raw value — never silently hidden,
/// in case a code outside this confirmed set shows up later.
/// </summary>
public static class QueueStatusLabel
{
    private static readonly Dictionary<string, string> KnownCodes = new(StringComparer.OrdinalIgnoreCase)
    {
        ["S"] = "ใหม่",
        ["L"] = "แล็ป",
        ["F"] = "การเงิน",
        ["P"] = "ห้องยา",
    };

    public static string ToLabel(string? rawStatus)
    {
        if (string.IsNullOrWhiteSpace(rawStatus))
        {
            return "รอเรียกคิว";
        }

        return KnownCodes.TryGetValue(rawStatus.Trim(), out var label) ? label : rawStatus;
    }
}

/// <summary>
/// "Currently serving" + "estimated wait" derived from today's real qtimecall timestamps in the
/// same queue group — never a made-up number. Needs at least 2 calls today (1 real gap) before an
/// estimate is offered at all.
/// </summary>
public static class QueueWaitEstimate
{
    public static (string? CurrentServing, int? EstimatedWaitMinutes, int SampleSize) Compute(
        IReadOnlyList<CalledRow> calledToday,
        int aheadCount)
    {
        var times = calledToday
            .Select(r => (r.QNumber, At: QueueTimeFormat.Parse(r.QTimeCall)))
            .Where(r => r.At.HasValue)
            .OrderBy(r => r.At!.Value)
            .ToList();

        string? currentServing = times.Count > 0 ? times[^1].QNumber : null;

        if (times.Count < 2 || aheadCount <= 0)
        {
            return (currentServing, null, 0);
        }

        var gaps = new List<double>();
        for (var i = 1; i < times.Count; i++)
        {
            gaps.Add((times[i].At!.Value - times[i - 1].At!.Value).TotalMinutes);
        }

        var averageMinutesPerCase = gaps.Average();
        var estimatedMinutes = (int)Math.Round(averageMinutesPerCase * aheadCount);
        return (currentServing, estimatedMinutes, gaps.Count);
    }
}
