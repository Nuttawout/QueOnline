namespace QueOnline.Api.Models;

public sealed class WebPushOptions
{
    public string PublicKey { get; set; } = "";
    public string PrivateKey { get; set; } = "";
    public string Subject { get; set; } = "mailto:admin@queonline.local";
}

/// <summary>
/// Same 3-tier urgency ladder as client/src/composables/useQueueAlert.js, ported server-side so
/// the background watcher can decide when to push even after the browser tab is closed.
/// </summary>
public static class QueueAlertLevel
{
    public const int None = 0;
    public const int Ahead2 = 1;
    public const int Ahead1 = 2;
    public const int Arrived = 3;

    public static int Compute(int aheadCount, bool arrived)
    {
        if (arrived) return Arrived;
        if (aheadCount <= 1) return Ahead1;
        if (aheadCount <= 2) return Ahead2;
        return None;
    }

    /// <summary>
    /// arrivedTitle/arrivedBody come from AppSettingsStore (admin-editable) — pass the app's
    /// current values, not a hardcoded default, so push notifications match what the in-page
    /// alert says (client reads the same setting via GET /api/settings).
    /// </summary>
    public static (string Title, string? Body) Message(int level, string? label, string arrivedTitle, string arrivedBody)
    {
        return level switch
        {
            Arrived => (arrivedTitle, label is null ? arrivedBody : $"{arrivedBody} คิวหมายเลข {label}"),
            Ahead1 => ("อีก 1 คิวจะถึงคิวของท่าน", label is null ? null : $"คิวหมายเลข {label} เตรียมตัวได้เลย"),
            Ahead2 => ("อีก 2 คิวจะถึงคิวของท่าน", label is null ? null : $"คิวหมายเลข {label} เตรียมตัวได้เลย"),
            _ => throw new ArgumentOutOfRangeException(nameof(level), level, "No message for this level."),
        };
    }
}

public sealed record PushSubscriptionKeysDto(string P256dh, string Auth);

public sealed record PushSubscriptionPayloadDto(string Endpoint, PushSubscriptionKeysDto Keys);

public sealed record SubscribeRequest(PushSubscriptionPayloadDto Subscription, string? Hn, string? QNumber);

public sealed record UnsubscribeRequest(string Endpoint);

public sealed class PushSubscriptionRecord
{
    public long Id { get; set; }
    public string? Hn { get; set; }
    public string? QNumber { get; set; }
    public required string Endpoint { get; set; }
    public required string P256dh { get; set; }
    public required string Auth { get; set; }
    public int LastLevel { get; set; }
}
