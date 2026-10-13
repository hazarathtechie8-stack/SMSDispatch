using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Serialization;
using SmsDispatch.Functions.Models;

namespace SmsDispatch.Functions.Functions;

/// <summary>
/// Independently receives every message on the audit-logger subscription.
/// Logs for compliance — never throws.
/// </summary>
public class AuditLogger(ILogger<AuditLogger> logger)
{
    private static readonly JsonSerializerOptions Opts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters           = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    [Function(nameof(AuditLogger))]
    public Task Run(
        [ServiceBusTrigger(
            topicName:        "sms-dispatch",
            subscriptionName: "audit-logger",
            Connection = "ServiceBusConnection")]
        string messageBody,
        FunctionContext context)
    {
        try
        {
            var msg = JsonSerializer.Deserialize<BusMessage>(messageBody, Opts);
            if (msg is not null)
                logger.LogInformation(
                    "[AUDIT] Id={Id} To={To} QueuedAt={At} MsgLen={Len}",
                    msg.Id, msg.ToPhoneNumber, msg.QueuedAt, msg.Message?.Length ?? 0);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[AUDIT] Failed to deserialize");
        }
        return Task.CompletedTask;
    }
}
