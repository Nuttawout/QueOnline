using Microsoft.AspNetCore.Mvc;
using QueOnline.Api.Data;

namespace QueOnline.Api.Controllers;

/// <summary>Public read-only settings the main queue-status page renders itself with.</summary>
[ApiController]
[Route("api/{branch}/[controller]")]
public sealed class SettingsController : ControllerBase
{
    private readonly IAppSettingsStore _store;
    private readonly IBranchContext _branchContext;

    public SettingsController(IAppSettingsStore store, IBranchContext branchContext)
    {
        _store = store;
        _branchContext = branchContext;
    }

    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        var settings = await _store.GetAsync(_branchContext.Current.Slug, ct);
        return Ok(settings);
    }
}
