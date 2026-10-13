using MediatR;
using SmsDispatch.Api.Models;

namespace SmsDispatch.Api.Queries;

/// <summary>
/// Query: paginated, filtered, sorted list of messages.
/// </summary>
public record GetMessagesQuery(
    string? Search   = null,
    string  Criteria = "contains",
    string  SortBy   = "createdDateTime",
    string  SortDir  = "desc",
    int     Page     = 1,
    int     PageSize = 20) : IRequest<PagedResult<MessageResponse>>;
