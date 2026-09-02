using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using QueOnline.Api.Data;
using QueOnline.Api.Models;

namespace QueOnline.Api.Controllers;

[ApiController]
[Route("api/{branch}/[controller]")]
public sealed class PushController : ControllerBase
{
    private readonly IPushSubscriptionStore _store;
    private readonly IBranchContext _branchContext;
    private readonly WebPushOptions _webPushOptions;

    public PushController(IPushSubscriptionStore store, IBranchContext branchContext, IOptions<WebPushOptions> webPushOptions)
    {
        _store = store;
        _branchContext = branchContext;
        _webPushOptions = webPushOptions.Value;
    }

    [HttpGet("public-key")]
    public IActionResult GetPublicKey() => Ok(new { publicKey = _webPushOptions.PublicKey });

    [HttpPost("subscribe")]
    public async Task<IActionResult> Subscribe([FromBody] SubscribeRequest request, CancellationToken ct)
    {
        if (request.Subscription is null || string.IsNullOrWhiteSpace(request.Subscription.Endpoint))
        {
            return BadRequest(new { message = "ข้อมูล subscription ไม่ถูกต้อง" });
        }

        if (string.IsNullOrWhiteSpace(request.Hn) && string.IsNullOrWhiteSpace(request.QNumber))
        {
            return BadRequest(new { message = "ระบุ hn หรือ qnumber อย่างน้อยหนึ่งค่า" });
        }

        await _store.InitializeAsync();
        await _store.UpsertAsync(_branchContext.Current.Slug, request.Hn, request.QNumber, request.Subscription, ct);
        return Ok();
    }

    [HttpPost("unsubscribe")]
    public async Task<IActionResult> Unsubscribe([FromBody] UnsubscribeRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Endpoint))
        {
            return BadRequest(new { message = "ระบุ endpoint" });
        }

        await _store.RemoveAsync(_branchContext.Current.Slug, request.Endpoint, ct);
        return Ok();
    }
}
