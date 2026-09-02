namespace QueOnline.Api.Models;

/// <summary>One hospital branch — its own Que_lcb SQL Server, its own admin password, its own
/// branding/settings. Stored/managed via BranchRegistry (server/App_Data/branches.db) — never
/// hardcoded, never exposed to the client beyond Slug/DisplayName (see BranchController) except to
/// an authenticated super-admin (see SuperAdminController).
///
/// DB fields are kept structured (ServerName/DatabaseName/DbLogin/DbPassword/EncryptTransfer)
/// rather than one free-text connection string, matching how the hospital IT side hands these over
/// (Server Name / Database Name / Login / Password / a "Transfer Mode" on-off toggle) and how
/// SSMS's own connection dialog presents them — TrustServerCertificate is always true (these are
/// on-prem servers reached by IP, not a publicly-trusted cert), EncryptTransfer is the one thing
/// that varies per branch and maps to ADO.NET's Encrypt option.</summary>
public sealed record Branch(
    string Slug,
    string DisplayName,
    string ServerName,
    string DatabaseName,
    string DbLogin,
    string DbPassword,
    bool EncryptTransfer,
    string AdminPassword)
{
    public string ConnectionString =>
        $"Server={ServerName};Database={DatabaseName};User Id={DbLogin};Password={DbPassword};TrustServerCertificate=True;Encrypt={EncryptTransfer}";
}

/// <summary>What the public, unauthenticated branch-picker endpoint returns — deliberately just
/// enough to render a list, never any DB or admin credential.</summary>
public sealed record BranchSummary(string Slug, string DisplayName);

public sealed record SuperAdminLoginRequest(string Password);
public sealed record SuperAdminLoginResponse(string Token);

/// <summary>Body for creating/updating a branch via the super-admin API — the slug itself comes
/// from the route (PUT api/super-admin/branches/{slug}), not this body, and is immutable once a
/// branch exists: it's the primary key everything else (AppSettings rows, uploaded file names,
/// admin session tokens) is keyed by, so changing it in place would silently orphan that data.</summary>
public sealed record UpsertBranchRequest(
    string DisplayName,
    string ServerName,
    string DatabaseName,
    string DbLogin,
    string DbPassword,
    bool EncryptTransfer,
    string AdminPassword);
