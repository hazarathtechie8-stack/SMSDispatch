using MediatR;
using SmsDispatch.Api.Data;

namespace SmsDispatch.Api.Commands;

/// <summary>
/// Handles UpdateMessageStatusCommand:
///   Finds the message by ID and updates its status, error message, and external SID.
/// </summary>
public class UpdateMessageStatusCommandHandler(
    SmsDbContext db,
    ILogger<UpdateMessageStatusCommandHandler> logger)
    : IRequestHandler<UpdateMessageStatusCommand, bool>
{
    public async Task<bool> Handle(UpdateMessageStatusCommand cmd, CancellationToken ct)
    {
        var entity = await db.Messages.FindAsync([(decimal)cmd.Id], ct);
        if (entity is null)
        {
            logger.LogWarning("UpdateMessageStatus: message {Id} not found", cmd.Id);
            return false;
        }

        entity.Status           = cmd.Status.ToString();
        entity.ErrorMessage     = cmd.ErrorMessage;
        entity.ExternalSid      = cmd.TwilioSid;
        entity.ModifiedDatetime = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);

        logger.LogInformation("Message {Id} status updated to {Status}", cmd.Id, cmd.Status);
        return true;
    }
}
