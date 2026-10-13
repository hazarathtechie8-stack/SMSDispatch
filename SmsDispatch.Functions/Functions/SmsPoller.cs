using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SmsDispatch.Functions.Data;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SmsDispatch.Functions.Functions;

/// <summary>
/// Timer trigger — fires every 60 seconds.
/// Reads Pending rows from SQL, marks them Queued, publishes to Service Bus topic.
/// Recovery net for messages that failed to publish via the Web API.
/// </summary>
public class SmsPoller(
    FunctionsDbContext   db,
    ServiceBusClient     sbClient,
    IConfiguration       cfg,
    ILogger<SmsPoller>   logger)
{
    private static readonly JsonSerializerOptions Opts = new()
    {
        PropertyNamingPolicy   = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    [Function(nameof(SmsPoller))]
    public async Task Run(
        [TimerTrigger("0 */1 * * * *")] TimerInfo timer,
        FunctionContext context)
    {
        var topicName = cfg["AzureServiceBus:TopicName"] ?? "sms-dispatch";
        await using var sender = sbClient.CreateSender(topicName);

        var pending = await db.Messages
            .Where(m => m.Status == "Pending")
            .OrderBy(m => m.CreatedDateTime)
            .Take(500)
            .ToListAsync();

        if (pending.Count == 0)
        {
            logger.LogDebug("SmsPoller: no pending messages.");
            return;
        }

        logger.LogInformation("SmsPoller: publishing {Count} pending messages", pending.Count);

        var batch    = await sender.CreateMessageBatchAsync();
        var overflow = new List<string>();

        foreach (var m in pending)
        {
            m.Status           = "Queued";
            m.ModifiedDatetime = DateTime.UtcNow;

            var payload = new
            {
                id              = (long)m.Id,
                toPhoneNumber   = m.To.ToString("F0"),
                fromPhoneNumber = m.From.ToString("F0"),
                message         = m.Message,
                recipientName   = m.RecipientName,
                queuedAt        = m.CreatedDateTime
            };

            var sbMsg = new ServiceBusMessage(JsonSerializer.Serialize(payload, Opts))
            {
                ContentType   = "application/json",
                MessageId     = ((long)m.Id).ToString(),
                CorrelationId = ((long)m.Id).ToString()
            };

            if (!batch.TryAddMessage(sbMsg))
                overflow.Add(m.Id.ToString());
        }

        await db.SaveChangesAsync();
        await sender.SendMessagesAsync(batch);

        if (overflow.Count > 0)
            logger.LogWarning(
                "SmsPoller: {Count} messages did not fit in batch; picked up next tick.",
                overflow.Count);
    }
}
