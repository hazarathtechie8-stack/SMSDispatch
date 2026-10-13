using MediatR;
using SmsDispatch.Api.Data;
using SmsDispatch.Api.Services;

namespace SmsDispatch.Api.Commands;

/// <summary>
/// Handles SendMessageCommand:
///   1. Validates phone numbers
///   2. Persists to SQL with Status=Pending
///   3. Publishes to Service Bus → Status=Queued
///   4. On publish failure, leaves as Pending (SmsPoller picks it up in 60 s)
/// </summary>
public class SendMessageCommandHandler(
    SmsDbContext            db,
    IServiceScopeFactory scopeFactory,
    ILogger<SendMessageCommandHandler> logger)
    : IRequestHandler<SendMessageCommand, SendMessageResult>
{
    public async Task<SendMessageResult> Handle(SendMessageCommand cmd, CancellationToken ct)
    {
        var now = DateTime.UtcNow;

        var entity = new MessageEntity
        {
            To = decimal.Parse(cmd.ToPhoneNumber),
            From = decimal.Parse(cmd.FromPhoneNumber),
            Message = cmd.Message,
            RecipientName = cmd.RecipientName,
            Status = "Pending",
            CreatedDateTime = now,
            ModifiedDatetime = now
        };

        // 1. Write to SQL — source of truth before anything touches the bus
        db.Messages.Add(entity);
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Saved message {Id} with status Pending", (long)entity.Id);

        // Return Immediately - don't wait for Service Bus
        var savedId = (long)entity.Id;

        // 2.Publish to Service Bus topic "sms-dispatch"
        _ = Task.Run(async () =>
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var scopedDb = scope.ServiceProvider.GetRequiredService<SmsDbContext>();
            var scopedPublisher = scope.ServiceProvider.GetRequiredService<IServiceBusPublisher>();

            var scopedEntity = await scopedDb.Messages.FindAsync([(decimal)savedId]);
            if (scopedEntity is null) return;
            try
            {
                await scopedPublisher.PublishAsync(scopedEntity);

                // 3. Mark Queued now that it's on the bus
                scopedEntity.Status = "Queued";
                scopedEntity.ModifiedDatetime = DateTime.UtcNow;
                await scopedDb.SaveChangesAsync();

                logger.LogInformation("Message {Id} published to Service Bus, status → Queued", (long)entity.Id);
            }
            catch (Exception ex)
            {
                // Publish failed — leave as Pending so SmsPoller picks it up on next 60 s tick
                logger.LogError(ex,
                    "Failed to publish message {Id} to Service Bus; will retry via poller",
                    (long)entity.Id);
            }          

        }, CancellationToken.None);

        return new SendMessageResult(savedId, entity.Status);
    }
}
