using MediatR;
using Microsoft.AspNetCore.Mvc;
using SmsDispatch.Api.Commands;
using SmsDispatch.Api.Models;
using SmsDispatch.Api.Queries;

namespace SmsDispatch.Api.Controllers;

/// <summary>
/// Thin controller — validates input, dispatches to MediatR, maps results to HTTP responses.
/// Zero business logic lives here; all work is in Command/Query handlers.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class MessagesController(IMediator mediator) : ControllerBase
{
    // ── POST /api/messages  (JSON body) ──────────────────────────────
    [HttpPost]
    [Consumes("application/json")]
    public Task<IActionResult> PostJson([FromBody] SendRequest req, CancellationToken ct)
        => Send(req.ToPhoneNumber, req.FromPhoneNumber, req.Message, req.RecipientName, ct);

    // ── POST /api/messages/form  (form-data / url-encoded) ───────────
    [HttpPost("form")]
    [Consumes("application/x-www-form-urlencoded", "multipart/form-data")]
    public Task<IActionResult> PostForm([FromForm] SendRequest req, CancellationToken ct)
        => Send(req.ToPhoneNumber, req.FromPhoneNumber, req.Message, req.RecipientName, ct);

    // ── GET /api/messages/send?to=&from=&message= ────────────────────
    [HttpGet("send")]
    public Task<IActionResult> PostQueryString([FromQuery] SendQueryRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.To) ||
            string.IsNullOrWhiteSpace(req.From) ||
            string.IsNullOrWhiteSpace(req.Message))
            return Task.FromResult<IActionResult>(BadRequest("to, from, and message are required."));

        return Send(req.To, req.From, req.Message, req.RecipientName, ct);
    }

    // ── GET /api/messages ────────────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> Search(
        [FromQuery] string? search   = null,
        [FromQuery] string  criteria = "contains",
        [FromQuery] string  sortBy   = "createdDateTime",
        [FromQuery] string  sortDir  = "desc",
        [FromQuery] int     page     = 1,
        [FromQuery] int     pageSize = 20,
        CancellationToken   ct       = default)
    {
        var result = await mediator.Send(
            new GetMessagesQuery(search, criteria, sortBy, sortDir, page, pageSize), ct);

        return Ok(result);
    }

    // ── GET /api/messages/{id} ───────────────────────────────────────
    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetById(long id, CancellationToken ct)
    {
        var result = await mediator.Send(new GetMessageByIdQuery(id), ct);
        return result is null ? NotFound() : Ok(result);
    }

    // ── PUT /api/messages/{id}/status  (called by Azure Function) ────
    [HttpPut("{id:long}/status")]
    public async Task<IActionResult> UpdateStatus(
        long id, [FromBody] StatusUpdate update, CancellationToken ct)
    {
        var found = await mediator.Send(
            new UpdateMessageStatusCommand(id, update.Status, update.ErrorMessage, update.TwilioSid), ct);

        return found ? NoContent() : NotFound();
    }

    // ── shared input validation before dispatching the command ────────
    private async Task<IActionResult> Send(
        string to, string from, string message, string? recipient, CancellationToken ct)
    {
        if (!decimal.TryParse(to, out _))
            return BadRequest("'to' must be a numeric phone number.");
        if (!decimal.TryParse(from, out _))
            return BadRequest("'from' must be a numeric phone number.");
        if (string.IsNullOrWhiteSpace(message))
            return BadRequest("'message' is required.");
        if (message.Length > 1000)
            return BadRequest("'message' exceeds 1000 characters.");

        var result = await mediator.Send(new SendMessageCommand(to, from, message, recipient), ct);
        return Accepted(new { id = result.Id, status = result.Status });
    }
}
