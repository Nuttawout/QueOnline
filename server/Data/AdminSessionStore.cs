using System.Collections.Concurrent;
using System.Security.Cryptography;

namespace QueOnline.Api.Data;

/// <summary>
/// One admin password PER BRANCH (no user accounts within a branch) — a successful login gets an
/// opaque bearer token held in memory only (lost on API restart, which just means logging in
/// again; this is a low-stakes cosmetic settings panel, not a system worth building full user auth
/// for). Each token is bound to the branch it was issued for — AdminController.IsAuthorized checks
/// the token's branch matches the CURRENT request's {branch} route value, so a token from one
/// branch's login can never be replayed against another branch's admin API.
/// </summary>
public interface IAdminSessionStore
{
    string CreateToken(string branchSlug);
    bool IsValidForBranch(string? token, string branchSlug);
}

public sealed class AdminSessionStore : IAdminSessionStore
{
    private static readonly TimeSpan SessionLifetime = TimeSpan.FromHours(12);
    private readonly ConcurrentDictionary<string, (string BranchSlug, DateTime Expiry)> _tokens = new();

    public string CreateToken(string branchSlug)
    {
        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .Replace('+', '-').Replace('/', '_').TrimEnd('=');
        _tokens[token] = (branchSlug, DateTime.UtcNow.Add(SessionLifetime));
        return token;
    }

    public bool IsValidForBranch(string? token, string branchSlug)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        if (!_tokens.TryGetValue(token, out var entry))
        {
            return false;
        }

        if (entry.Expiry < DateTime.UtcNow)
        {
            _tokens.TryRemove(token, out _);
            return false;
        }

        return string.Equals(entry.BranchSlug, branchSlug, StringComparison.OrdinalIgnoreCase);
    }
}
