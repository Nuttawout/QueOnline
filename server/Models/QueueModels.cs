namespace QueOnline.Api.Models;

public sealed record DepartmentOption(string Code, string Name);

/// <summary>Skipped = a LATER step already has a timestamp while this one never got one — the
/// visit went straight past this stage (e.g. no lab test ordered) rather than this stage just not
/// having happened yet. Computed server-side (QueueRepository.BuildTimeline) so it's one source of
/// truth instead of every client re-deriving it.
///
/// AheadCount/EstimatedWaitMinutes are populated ONLY on lab/fin/pha when that step is the current
/// one (not done, not skipped, and the visit has already been called into the exam room) — an
/// approximation, confirmed acceptable with the hospital (2026-09-01): Queue_q has no field saying
/// in advance whether a visit will ever reach a given stage (e.g. "will need a lab test"), so the
/// count can include people who turn out to skip that stage entirely. Same shape as the top-level
/// AheadCount/EstimatedWaitMinutes on QueueLookupResult, which stay exactly as they were (the wait
/// before being called into the exam room) — this is additional, not a replacement.</summary>
public sealed record QueueTimelineStep(
    string Key, string Label, string? Location, DateTime? At, bool Done, bool Skipped,
    int? AheadCount = null, int? EstimatedWaitMinutes = null);

public sealed record BoardQueueItem(
    int Qid,
    string? QNumber,
    string? PatientName,
    string? DepartmentName,
    string? GroupCode,
    string? GroupName,
    string? Counter,
    string RawStatus,
    string StatusLabel,
    DateTime? RegisteredAt,
    DateTime? CalledAt
);

public sealed record QueueLookupResult(
    int Qid,
    string? QNumber,
    string? PatientName,
    string? Hn,
    string? VisitNumber,
    string? DepartmentName,
    string? GroupCode,
    string? GroupName,
    string RawStatus,
    string StatusLabel,
    int AheadCount,
    List<QueueTimelineStep> Timeline,
    /// <summary>Queue number most recently called in the same group today — null if none called yet.</summary>
    string? CurrentServingNumber,
    /// <summary>Estimated minutes until called, based on today's actual average gap between calls
    /// in the same group. Null when there isn't enough real data yet (fewer than 2 calls today) —
    /// never a guessed/fabricated number.</summary>
    int? EstimatedWaitMinutes,
    /// <summary>How many real call-to-call gaps today the estimate above is averaged from.</summary>
    int EstimatedWaitSampleSize
);

/// <summary>One row of {queue number, call time} used to compute CurrentServingNumber/EstimatedWaitMinutes.</summary>
public sealed class CalledRow
{
    public string? QNumber { get; set; }
    public string? QTimeCall { get; set; }
}

/// <summary>
/// Raw Queue_q row shape before mapping into API DTOs. Field mapping confirmed directly by the
/// hospital (2026-08-31) — some of these differ from what the old stored procedures implied:
/// registration time is <c>qdatetime</c> (NOT <c>qtimeregister</c>, which the legacy display
/// procs used but isn't the field this app should read), and the patient's HN is
/// <c>qocmchatnum</c> (NOT <c>qocmnum</c>, which is a separate internal visit id — exposed here
/// as VisitNumber-adjacent but the actual visit number is <c>qocmVst</c>).
/// </summary>
public sealed class QueueRow
{
    public int Qid { get; set; }
    public string? QNumber { get; set; }
    public string? QDep { get; set; }
    public string? QStatus { get; set; }
    public string? QOcmFullName { get; set; }
    public string? QOcmChatNum { get; set; }
    public string? QOcmVst { get; set; }
    public string? QCounter { get; set; }
    public string? QDateTime { get; set; }
    public string? QTimeCall { get; set; }
    public string? QTimeCallLab { get; set; }
    public string? QRoomLab { get; set; }
    public string? QTimeCallFin { get; set; }
    public string? QRoomFin { get; set; }
    public string? QTimeCallPha { get; set; }
    public string? QRoomPha { get; set; }
    public string? GruCode { get; set; }
    public string? GruGroup { get; set; }
    public string? DepName { get; set; }
}
