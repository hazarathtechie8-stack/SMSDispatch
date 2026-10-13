namespace SmsDispatch.Api.Models;

// ── Inbound request models ────────────────────────────────────────────────────

/// <summary>POST body — also used for form-data binding.</summary>
public record SendRequest(
    string  ToPhoneNumber,
    string  FromPhoneNumber,
    string  Message,
    string? RecipientName = null);

/// <summary>Query-string variant (all fields individually bound).</summary>
public record SendQueryRequest(
    string? To,
    string? From,
    string? Message,
    string? RecipientName);

// ── Status / domain enums ─────────────────────────────────────────────────────

public enum MessageStatus
{
    Pending,
    Queued,
    Sending,
    Sent,
    Failed,
    DeadLettered
}

// ── Outbound response models ──────────────────────────────────────────────────

public record MessageResponse(
    long     Id,
    string   ToPhoneNumber,
    string   FromPhoneNumber,
    string   Message,
    string?  RecipientName,
    string   Status,
    string?  ErrorMessage,
    DateTime CreatedDateTime,
    DateTime ModifiedDatetime);

public record StatusUpdate(
    MessageStatus Status,
    string?       ErrorMessage = null,
    string?       TwilioSid   = null);

public record PagedResult<T>(
    IEnumerable<T> Items,
    int            TotalCount,
    int            Page,
    int            PageSize);
