using Microsoft.Extensions.Options;
using QueOnline.Api.Data;
using QueOnline.Api.Models;
using WebPush;

namespace QueOnline.Api.Services;

/// <summary>
/// Polls every subscribed queue number, ACROSS EVERY CONFIGURED BRANCH, on the same 10s cadence
/// the frontend UI polls at, and sends a real Web Push notification (works even with the browser
/// tab fully closed, as long as the OS/browser push service is reachable) when the alert level
/// advances. Mirrors the exact 3-tier message ladder in client/src/composables/useQueueAlert.js so
/// foreground (in-page) and background (push) alerts always say the same thing.
/// </summary>
public sealed class QueueAlertBackgroundService : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(10);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IBranchRegistry _branchRegistry;
    private readonly WebPushOptions _webPushOptions;
    private readonly ILogger<QueueAlertBackgroundService> _logger;

    public QueueAlertBackgroundService(
        IServiceScopeFactory scopeFactory,
        IBranchRegistry branchRegistry,
        IOptions<WebPushOptions> webPushOptions,
        ILogger<QueueAlertBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _branchRegistry = branchRegistry;
        _webPushOptions = webPushOptions.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using (var scope = _scopeFactory.CreateScope())
        {
            var store = scope.ServiceProvider.GetRequiredService<IPushSubscriptionStore>();
            await store.InitializeAsync();
        }

        if (string.IsNullOrWhiteSpace(_webPushOptions.PrivateKey))
        {
            _logger.LogWarning("WebPush:PrivateKey is not configured — push notifications are disabled.");
            return;
        }

        var vapidDetails = new VapidDetails(_webPushOptions.Subject, _webPushOptions.PublicKey, _webPushOptions.PrivateKey);
        var webPushClient = new WebPushClient();

        using var timer = new PeriodicTimer(PollInterval);
        do
        {
            // Each branch gets its own scope (and therefore its own IBranchContext/IQueueRepository
            // pointed at that branch's own SQL Server) — one branch's poll failing must never skip
            // the rest, so each iteration has its own try/catch rather than one wrapping the loop.
            var branches = await _branchRegistry.GetAllAsync(stoppingToken);
            foreach (var branch in branches)
            {
                try
                {
                    await TickAsync(branch, webPushClient, vapidDetails, stoppingToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogError(ex, "Queue alert poll failed for branch {Branch}.", branch.Slug);
                }
            }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task TickAsync(Branch branch, WebPushClient webPushClient, VapidDetails vapidDetails, CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        scope.ServiceProvider.GetRequiredService<IBranchContext>().Current = branch;
        var store = scope.ServiceProvider.GetRequiredService<IPushSubscriptionStore>();
        var queueRepository = scope.ServiceProvider.GetRequiredService<IQueueRepository>();
        var settingsStore = scope.ServiceProvider.GetRequiredService<IAppSettingsStore>();

        var subscriptions = await store.GetAllForBranchAsync(branch.Slug, ct);
        if (subscriptions.Count == 0)
        {
            return;
        }

        var settings = await settingsStore.GetAsync(branch.Slug, ct);
        foreach (var sub in subscriptions)
        {
            ct.ThrowIfCancellationRequested();

            try
            {
                var result = await queueRepository.LookupAsync(sub.Hn, sub.QNumber, ct);

                if (result is null)
                {
                    // Not found today — either the queue rolled over to a new day or the number
                    // was never valid. Either way there is nothing left to watch.
                    await store.RemoveAsync(branch.Slug, sub.Endpoint, ct);
                    continue;
                }

                var arrived = result.Timeline.FirstOrDefault(s => s.Key == "call")?.Done ?? false;
                var level = QueueAlertLevel.Compute(result.AheadCount, arrived);

                if (level <= sub.LastLevel || level == QueueAlertLevel.None)
                {
                    continue;
                }

                var (title, body) = QueueAlertLevel.Message(level, result.QNumber, settings.ArrivedTitle, settings.ArrivedBody);
                var sent = await SendAsync(branch.Slug, webPushClient, vapidDetails, sub, title, body, ct);

                if (sent)
                {
                    await store.UpdateLevelAsync(sub.Id, level, ct);
                }

                // One-shot: once the "arrived" push is delivered there is nothing further to watch for.
                if (level == QueueAlertLevel.Arrived && sent)
                {
                    await store.RemoveAsync(branch.Slug, sub.Endpoint, ct);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // One malformed/misbehaving subscription must never block the rest of the batch.
                _logger.LogError(ex, "Failed processing subscription {Id} (branch {Branch}).", sub.Id, branch.Slug);
            }
        }
    }

    private async Task<bool> SendAsync(
        string branchSlug,
        WebPushClient webPushClient,
        VapidDetails vapidDetails,
        PushSubscriptionRecord sub,
        string title,
        string? body,
        CancellationToken ct)
    {
        var pushSubscription = new PushSubscription(sub.Endpoint, sub.P256dh, sub.Auth);
        var payload = System.Text.Json.JsonSerializer.Serialize(new { title, body });

        try
        {
            await webPushClient.SendNotificationAsync(pushSubscription, payload, vapidDetails, ct);
            _logger.LogInformation("Push sent to subscription {Id}: {Title}", sub.Id, title);
            return true;
        }
        catch (WebPushException ex) when (ex.StatusCode is System.Net.HttpStatusCode.NotFound or System.Net.HttpStatusCode.Gone)
        {
            // Browser unsubscribed or the subscription expired — stop tracking it.
            var scope = _scopeFactory.CreateScope();
            var store = scope.ServiceProvider.GetRequiredService<IPushSubscriptionStore>();
            await store.RemoveAsync(branchSlug, sub.Endpoint, ct);
            return false;
        }
        catch (WebPushException ex)
        {
            _logger.LogWarning(ex, "Push send failed for subscription {Id}.", sub.Id);
            return false;
        }
        catch (Exception ex)
        {
            // Malformed subscription keys (e.g. WebPush.Model.InvalidEncryptionDetailsException)
            // will never succeed on retry — remove it so it doesn't keep failing every tick and
            // blocking the rest of the batch. Any other unexpected error is logged and skipped;
            // the subscription stays and gets retried next tick.
            _logger.LogWarning(ex, "Unexpected error sending push for subscription {Id}.", sub.Id);
            if (ex is WebPush.Model.InvalidEncryptionDetailsException)
            {
                var scope = _scopeFactory.CreateScope();
                var store = scope.ServiceProvider.GetRequiredService<IPushSubscriptionStore>();
                await store.RemoveAsync(branchSlug, sub.Endpoint, ct);
            }
            return false;
        }
    }
}
