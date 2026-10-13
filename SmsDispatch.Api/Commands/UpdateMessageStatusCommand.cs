using MediatR;
using SmsDispatch.Api.Models;

namespace SmsDispatch.Api.Commands;

/// <summary>
/// Command: update the status of an existing message (called by Azure Function).
/// Returns true if found and updated, false if not found.
/// </summary>
public record UpdateMessageStatusCommand(
    long          Id,
    MessageStatus Status,
    string?       ErrorMessage,
    string?       TwilioSid) : IRequest<bool>;
