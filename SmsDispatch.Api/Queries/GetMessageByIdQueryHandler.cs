using MediatR;
using SmsDispatch.Api.Data;
using SmsDispatch.Api.Models;

namespace SmsDispatch.Api.Queries;

/// <summary>
/// Handles GetMessageByIdQuery: finds a single message and maps it to MessageResponse.
/// </summary>
public class GetMessageByIdQueryHandler(SmsDbContext db)
    : IRequestHandler<GetMessageByIdQuery, MessageResponse?>
{
    public async Task<MessageResponse?> Handle(GetMessageByIdQuery qry, CancellationToken ct)
    {
        var m = await db.Messages.FindAsync([(decimal)qry.Id], ct);
        if (m is null) return null;

        return new MessageResponse(
            (long)m.Id,
            m.To.ToString("F0"),
            m.From.ToString("F0"),
            m.Message,
            m.RecipientName,
            m.Status,
            m.ErrorMessage,
            m.CreatedDateTime,
            m.ModifiedDatetime);
    }
}
