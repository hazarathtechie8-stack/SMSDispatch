using MediatR;
using SmsDispatch.Api.Models;

namespace SmsDispatch.Api.Commands;

/// <summary>
/// Command: save a new SMS message to SQL and publish to Service Bus.
/// Returned result carries the new message ID and its initial status.
/// </summary>
public record SendMessageCommand(
    string ToPhoneNumber,
    string FromPhoneNumber,
    string Message,
    string? RecipientName) : IRequest<SendMessageResult>;

public record SendMessageResult(long Id, string Status);
