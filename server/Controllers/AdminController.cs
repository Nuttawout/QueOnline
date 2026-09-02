using Microsoft.AspNetCore.Mvc;
using QueOnline.Api.Data;
using QueOnline.Api.Models;

namespace QueOnline.Api.Controllers;

[ApiController]
[Route("api/{branch}/[controller]")]
public sealed class AdminController : ControllerBase
{
    private static readonly HashSet<string> AllowedLogoExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".png", ".jpg", ".jpeg", ".svg", ".webp",
    };
    private static readonly HashSet<string> AllowedSoundExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".mp3", ".wav", ".ogg",
    };
    private const long MaxUploadBytes = 5 * 1024 * 1024; // 5 MB

    private readonly IAppSettingsStore _settingsStore;
    private readonly IAdminSessionStore _sessionStore;
    private readonly IBranchContext _branchContext;
    private readonly IWebHostEnvironment _environment;

    public AdminController(
        IAppSettingsStore settingsStore,
        IAdminSessionStore sessionStore,
        IBranchContext branchContext,
        IWebHostEnvironment environment)
    {
        _settingsStore = settingsStore;
        _sessionStore = sessionStore;
        _branchContext = branchContext;
        _environment = environment;
    }

    private bool IsAuthorized()
    {
        var header = Request.Headers.Authorization.ToString();
        if (!header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var token = header["Bearer ".Length..].Trim();
        // Bound to THIS request's branch — a token issued by logging into one branch is rejected
        // here if replayed against a different branch's admin API, even though the token itself
        // is still "valid" (unexpired) in the session store. This is the actual tenant-isolation
        // boundary; see the doc comment on IAdminSessionStore.
        return _sessionStore.IsValidForBranch(token, _branchContext.Current.Slug);
    }

    [HttpPost("login")]
    public IActionResult Login([FromBody] AdminLoginRequest request)
    {
        var branch = _branchContext.Current;
        if (string.IsNullOrEmpty(branch.AdminPassword))
        {
            return StatusCode(500, new { message = $"ยังไม่ได้ตั้งรหัสผ่านแอดมินของสาขา \"{branch.Slug}\" ใน branches.json" });
        }

        if (request.Password != branch.AdminPassword)
        {
            return Unauthorized(new { message = "รหัสผ่านไม่ถูกต้อง" });
        }

        var token = _sessionStore.CreateToken(branch.Slug);
        return Ok(new AdminLoginResponse(token));
    }

    [HttpPut("settings")]
    public async Task<IActionResult> UpdateSettings([FromBody] UpdateSettingsRequest request, CancellationToken ct)
    {
        if (!IsAuthorized())
        {
            return Unauthorized(new { message = "กรุณาเข้าสู่ระบบใหม่" });
        }

        var slug = _branchContext.Current.Slug;
        await _settingsStore.UpdateAsync(slug, request, ct);
        var updated = await _settingsStore.GetAsync(slug, ct);
        return Ok(updated);
    }

    [HttpPost("logo")]
    [RequestSizeLimit(MaxUploadBytes)]
    public async Task<IActionResult> UploadLogo(IFormFile file, CancellationToken ct)
    {
        if (!IsAuthorized())
        {
            return Unauthorized(new { message = "กรุณาเข้าสู่ระบบใหม่" });
        }

        var result = await SaveUploadedFileAsync(file, "logo", AllowedLogoExtensions, ".png .jpg .jpeg .svg .webp", ct);
        if (result.Error is not null)
        {
            return BadRequest(new { message = result.Error });
        }

        await _settingsStore.UpdateLogoUrlAsync(_branchContext.Current.Slug, result.Url!, ct);
        return Ok(new { logoUrl = result.Url });
    }

    [HttpPost("sound")]
    [RequestSizeLimit(MaxUploadBytes)]
    public async Task<IActionResult> UploadSound(IFormFile file, CancellationToken ct)
    {
        if (!IsAuthorized())
        {
            return Unauthorized(new { message = "กรุณาเข้าสู่ระบบใหม่" });
        }

        var result = await SaveUploadedFileAsync(file, "sound", AllowedSoundExtensions, ".mp3 .wav .ogg", ct);
        if (result.Error is not null)
        {
            return BadRequest(new { message = result.Error });
        }

        var slug = _branchContext.Current.Slug;
        await _settingsStore.UpdateCustomSoundUrlAsync(slug, result.Url!, ct);
        // Switch the active choice to "custom" immediately — uploading a sound with no way to
        // hear it (still set to a preset) would be a confusing dead end.
        await _settingsStore.UpdateAsync(
            slug,
            new UpdateSettingsRequest(null, null, null, null, null, null, null, null, null, SoundChoice: "custom"),
            ct);
        var updated = await _settingsStore.GetAsync(slug, ct);
        return Ok(updated);
    }

    private async Task<(string? Url, string? Error)> SaveUploadedFileAsync(
        IFormFile file,
        string baseName,
        HashSet<string> allowedExtensions,
        string allowedExtensionsLabel,
        CancellationToken ct)
    {
        if (file is null || file.Length == 0)
        {
            return (null, "กรุณาเลือกไฟล์");
        }

        var ext = Path.GetExtension(file.FileName);
        if (!allowedExtensions.Contains(ext))
        {
            return (null, $"รองรับเฉพาะไฟล์ {allowedExtensionsLabel}");
        }

        var uploadsDir = Path.Combine(_environment.ContentRootPath, "App_Data", "uploads");
        Directory.CreateDirectory(uploadsDir);

        // Filename prefixed with the branch slug — one shared /media folder for every branch, so
        // without this a second branch's logo upload would silently overwrite the first branch's
        // file (same fixed "logo.<ext>" name for everyone). Old extensions from a previous upload
        // for THIS branch are cleaned up so stale files don't linger; other branches' files are
        // untouched.
        var slug = _branchContext.Current.Slug;
        var namePrefix = $"{slug}-{baseName}";
        foreach (var existingExt in allowedExtensions)
        {
            var existingPath = Path.Combine(uploadsDir, $"{namePrefix}{existingExt}");
            if (System.IO.File.Exists(existingPath))
            {
                System.IO.File.Delete(existingPath);
            }
        }

        var savePath = Path.Combine(uploadsDir, $"{namePrefix}{ext}");
        await using (var stream = System.IO.File.Create(savePath))
        {
            await file.CopyToAsync(stream, ct);
        }

        var url = $"/media/{namePrefix}{ext}?v={DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";
        return (url, null);
    }
}
