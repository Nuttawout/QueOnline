using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using QueOnline.Api.Data;
using QueOnline.Api.Models;

namespace QueOnline.Api.Controllers;

/// <summary>Manages the branch list itself (add/edit/remove a hospital branch, rotate its DB
/// connection string or admin password) — deliberately NOT branch-scoped (no {branch} in the
/// route) and gated by its own master password (SuperAdmin:Password in config), separate from any
/// branch's own admin password, because this surface can see/edit every branch's secrets at once.
/// See IBranchRegistry's doc comment for the storage/encryption details.</summary>
[ApiController]
[Route("api/super-admin")]
public sealed class SuperAdminController : ControllerBase
{
    private static readonly Regex SlugPattern = new("^[a-z0-9]+(-[a-z0-9]+)*$", RegexOptions.Compiled);
    private const string ReservedSlug = "super-admin"; // collides with the frontend's top-level /super-admin route

    private readonly IBranchRegistry _registry;
    private readonly ISuperAdminSessionStore _sessionStore;
    private readonly IConfiguration _configuration;

    public SuperAdminController(IBranchRegistry registry, ISuperAdminSessionStore sessionStore, IConfiguration configuration)
    {
        _registry = registry;
        _sessionStore = sessionStore;
        _configuration = configuration;
    }

    private bool IsAuthorized()
    {
        var header = Request.Headers.Authorization.ToString();
        if (!header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return _sessionStore.IsValid(header["Bearer ".Length..].Trim());
    }

    [HttpPost("login")]
    public IActionResult Login([FromBody] SuperAdminLoginRequest request)
    {
        var masterPassword = _configuration["SuperAdmin:Password"];
        if (string.IsNullOrEmpty(masterPassword))
        {
            return StatusCode(500, new { message = "ยังไม่ได้ตั้งรหัสผ่าน SuperAdmin:Password ใน config" });
        }

        if (request.Password != masterPassword)
        {
            return Unauthorized(new { message = "รหัสผ่านไม่ถูกต้อง" });
        }

        return Ok(new SuperAdminLoginResponse(_sessionStore.CreateToken()));
    }

    [HttpGet("branches")]
    public async Task<IActionResult> GetBranches(CancellationToken ct)
    {
        if (!IsAuthorized())
        {
            return Unauthorized(new { message = "กรุณาเข้าสู่ระบบใหม่" });
        }

        var branches = await _registry.GetAllAsync(ct);
        return Ok(branches.OrderBy(b => b.DisplayName, StringComparer.CurrentCultureIgnoreCase));
    }

    /// <summary>Create (new slug) or update (existing slug) a branch. The slug in the route is the
    /// only place a slug is ever set — see UpsertBranchRequest's doc comment for why it can't change
    /// once created.</summary>
    [HttpPut("branches/{slug}")]
    public async Task<IActionResult> UpsertBranch(string slug, [FromBody] UpsertBranchRequest request, CancellationToken ct)
    {
        if (!IsAuthorized())
        {
            return Unauthorized(new { message = "กรุณาเข้าสู่ระบบใหม่" });
        }

        if (!SlugPattern.IsMatch(slug) || string.Equals(slug, ReservedSlug, StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new { message = "รหัสสาขา (slug) ต้องเป็นตัวพิมพ์เล็ก a-z 0-9 และ - เท่านั้น และห้ามใช้คำว่า \"super-admin\"" });
        }

        if (string.IsNullOrWhiteSpace(request.DisplayName) || string.IsNullOrWhiteSpace(request.ServerName) ||
            string.IsNullOrWhiteSpace(request.DatabaseName) || string.IsNullOrWhiteSpace(request.DbLogin) ||
            string.IsNullOrWhiteSpace(request.DbPassword) || string.IsNullOrWhiteSpace(request.AdminPassword))
        {
            return BadRequest(new { message = "กรุณากรอกข้อมูลให้ครบทุกช่อง (ชื่อสาขา, Server, Database, Login, รหัสผ่าน DB, รหัสผ่าน admin)" });
        }

        await _registry.UpsertAsync(
            new Branch(slug, request.DisplayName, request.ServerName, request.DatabaseName, request.DbLogin, request.DbPassword, request.EncryptTransfer, request.AdminPassword),
            ct);
        return Ok();
    }

    [HttpDelete("branches/{slug}")]
    public async Task<IActionResult> DeleteBranch(string slug, CancellationToken ct)
    {
        if (!IsAuthorized())
        {
            return Unauthorized(new { message = "กรุณาเข้าสู่ระบบใหม่" });
        }

        var deleted = await _registry.DeleteAsync(slug, ct);
        if (!deleted)
        {
            return NotFound(new { message = $"ไม่พบสาขา \"{slug}\"" });
        }

        return Ok();
    }
}
