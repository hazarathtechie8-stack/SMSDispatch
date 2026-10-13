namespace SmsDispatch.Functions.Models;

public record BusMessage(
    long     Id,
    string   ToPhoneNumber,
    string   FromPhoneNumber,
    string   Message,
    string?  RecipientName,
    DateTime QueuedAt);

public record StatusUpdate(
    string  Status,
    string? ErrorMessage = null,
    string? TwilioSid = null);
