using Microsoft.AspNetCore.Mvc;
using QueOnline.Api.Data;
using QueOnline.Api.Models;

namespace QueOnline.Api.Controllers;

/// <summary>Public, unauthenticated, NOT branch-scoped (no {branch} in the route — this is what
/// lets the client discover which branches exist in the first place) — powers the branch-picker
/// landing page. Deliberately returns only Slug/DisplayName, never ConnectionString/AdminPassword;
/// see the doc comment on IBranchRegistry for why those never leave the server at all.</summary>
[ApiController]
[Route("api/branches")]
public sealed class BranchController : ControllerBase
{
    private readonly IBranchRegistry _registry;

    public BranchController(IBranchRegistry registry)
    {
        _registry = registry;
    }

    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        var branches = await _registry.GetAllAsync(ct);
        var summaries = branches
            .Select(b => new BranchSummary(b.Slug, b.DisplayName))
            .OrderBy(b => b.DisplayName, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
        return Ok(summaries);
    }
}
