using QueOnline.Api.Models;

namespace QueOnline.Api.Data;

/// <summary>Which branch the CURRENT HTTP request is for — set once per request by the branch-
/// resolution middleware in Program.cs (from the {branch} route value), read by QueueRepository
/// (needs the branch's own SQL Server connection string) and by branch-scoped controllers (need
/// Current.Slug to pass into AppSettingsStore/PushSubscriptionStore's branch-scoped methods).
/// Scoped, not Singleton — one HTTP request, one branch, never shared across requests.</summary>
public interface IBranchContext
{
    Branch Current { get; set; }
}

public sealed class BranchContext : IBranchContext
{
    private Branch? _current;

    public Branch Current
    {
        get => _current ?? throw new InvalidOperationException(
            "IBranchContext.Current read before the branch-resolution middleware set it — check Program.cs middleware ordering (must run after UseRouting, before MapControllers).");
        set => _current = value;
    }
}
