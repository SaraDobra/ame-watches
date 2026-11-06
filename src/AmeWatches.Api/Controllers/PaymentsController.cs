using System.Text;
using AmeWatches.Api.Services.Payments;
using Microsoft.AspNetCore.Mvc;

namespace AmeWatches.Api.Controllers;

public record SimulatorCompleteRequest(string Outcome);

[ApiController]
[Route("api/payments")]
public class PaymentsController : ControllerBase
{
    private const int MaxWebhookBytes = 16 * 1024;

    private readonly IPaymentService _payments;
    private readonly SimulatorPaymentProvider? _simulator;
    private readonly TimeProvider _time;

    public PaymentsController(IPaymentService payments, TimeProvider time, SimulatorPaymentProvider? simulator = null)
    {
        _payments = payments;
        _time = time;
        _simulator = simulator;
    }

    [HttpGet("methods")]
    public IActionResult Methods() => Ok(new { card = _payments.CardPaymentsEnabled });

    [HttpPost("webhook")]
    public async Task<IActionResult> Webhook()
    {
        if (Request.ContentLength > MaxWebhookBytes)
            return StatusCode(StatusCodes.Status413PayloadTooLarge);

        using var reader = new StreamReader(Request.Body, Encoding.UTF8);
        var body = await reader.ReadToEndAsync();
        if (body.Length > MaxWebhookBytes)
            return StatusCode(StatusCodes.Status413PayloadTooLarge);

        var result = await _payments.HandleWebhook(body, Request.Headers["X-Signature"].FirstOrDefault());
        return ToResponse(result);
    }

    [HttpGet("simulator/{sessionId}")]
    public async Task<IActionResult> SimulatorSession(string sessionId)
    {
        if (_simulator is null)
            return NotFound();

        var session = await _payments.GetSession(sessionId);
        return session is null ? NotFound() : Ok(session);
    }

    [HttpPost("simulator/{sessionId}/complete")]
    public async Task<IActionResult> SimulatorComplete(string sessionId, [FromBody] SimulatorCompleteRequest request)
    {
        if (_simulator is null)
            return NotFound();

        var session = await _payments.GetSession(sessionId);
        if (session is null)
            return NotFound();

        if (!Enum.TryParse<PaymentOutcome>(request.Outcome, true, out var outcome))
            return BadRequest(new { message = "Rezultati duhet të jetë Paid ose Failed." });

        var (body, signature) = _simulator.BuildWebhook(sessionId, outcome, session.Amount, session.Currency, _time.GetUtcNow());
        return ToResponse(await _payments.HandleWebhook(body, signature));
    }

    private IActionResult ToResponse(WebhookResult result) => result switch
    {
        WebhookResult.Accepted => Ok(new { status = "accepted" }),
        WebhookResult.Ignored => Ok(new { status = "ignored" }),
        WebhookResult.NotFound => NotFound(),
        _ => BadRequest(new { message = "Webhook i pavlefshëm." })
    };
}
