using MediatR;
using SmsDispatch.Api.Models;

namespace SmsDispatch.Api.Queries;

/// <summary>
/// Query: fetch a single message by its ID.
/// Returns null when not found (controller maps to 404).
/// </summary>
public record GetMessageByIdQuery(long Id) : IRequest<MessageResponse?>;
