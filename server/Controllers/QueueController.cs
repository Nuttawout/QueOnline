using Microsoft.AspNetCore.Mvc;
using QueOnline.Api.Data;

namespace QueOnline.Api.Controllers;

[ApiController]
[Route("api/{branch}/[controller]")]
public sealed class QueueController : ControllerBase
{
    private readonly IQueueRepository _repository;

    public QueueController(IQueueRepository repository)
    {
        _repository = repository;
    }

    /// <summary>Departments/groups for the board filter dropdown.</summary>
    [HttpGet("departments")]
    public async Task<IActionResult> GetDepartments(CancellationToken ct)
    {
        var departments = await _repository.GetDepartmentsAsync(ct);
        return Ok(departments);
    }

    /// <summary>Public "now serving" board for today, optionally filtered by department group code.</summary>
    [HttpGet("board")]
    public async Task<IActionResult> GetBoard([FromQuery] string? dep, CancellationToken ct)
    {
        var board = await _repository.GetBoardAsync(dep, ct);
        return Ok(board);
    }

    /// <summary>Personal queue status lookup by HN (qocmchatnum) or today's queue number.</summary>
    [HttpGet("lookup")]
    public async Task<IActionResult> Lookup([FromQuery] string? hn, [FromQuery] string? qnumber, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(hn) && string.IsNullOrWhiteSpace(qnumber))
        {
            return BadRequest(new { message = "ระบุ hn หรือ qnumber อย่างน้อยหนึ่งค่า" });
        }

        var result = await _repository.LookupAsync(hn, qnumber, ct);
        if (result is null)
        {
            return NotFound(new { message = "ไม่พบข้อมูลคิวของวันนี้" });
        }

        return Ok(result);
    }
}
