using System.Collections.Concurrent;
using System.Security.Cryptography;

namespace QueOnline.Api.Data;

/// <summary>
/// One master password for the whole app (SuperAdmin:Password in config — not a branch's
/// AdminPassword) — gates SuperAdminController, which can see/edit every branch's DB connection
/// string and admin password at once, so it deliberately does NOT share AdminSessionStore's
/// per-branch tokens. In-memory only, same low-stakes rationale as AdminSessionStore (lost on
/// restart just means logging in again).
/// </summary>
public interface ISuperAdminSessionStore
{
    string CreateToken();
    bool IsValid(string? token);
}

public sealed class SuperAdminSessionStore : ISuperAdminSessionStore
{
    private static readonly TimeSpan SessionLifetime = TimeSpan.FromHours(12);
    private readonly ConcurrentDictionary<string, DateTime> _tokens = new();

    public string CreateToken()
    {
        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .Replace('+', '-').Replace('/', '_').TrimEnd('=');
        _tokens[token] = DateTime.UtcNow.Add(SessionLifetime);
        return token;
    }

    public bool IsValid(string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        if (!_tokens.TryGetValue(token, out var expiry))
        {
            return false;
        }

        if (expiry < DateTime.UtcNow)
        {
            _tokens.TryRemove(token, out _);
            return false;
        }

        return true;
    }
}
