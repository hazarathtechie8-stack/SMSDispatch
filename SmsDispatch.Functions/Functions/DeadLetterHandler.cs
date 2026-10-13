using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using SmsDispatch.Functions.Models;

namespace SmsDispatch.Functions.Functions;

/// <summary>
/// Handles messages that exhausted all retries on the send-processor subscription.
/// Updates Web API status to DeadLettered.
/// </summary>
public class DeadLetterHandler(IHttpClientFactory httpFactory, ILogger<DeadLetterHandler> logger)
{
    private static readonly JsonSerializerOptions Opts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters           = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    [Function(nameof(DeadLetterHandler))]
    public async Task Run(
        [ServiceBusTrigger(
            topicName:        "sms-dispatch",
            subscriptionName: "send-processor/$DeadLetterQueue",
            Connection = "ServiceBusConnection")]
        string messageBody,
        FunctionContext context)
    {
        logger.LogWarning("DeadLetter received: {Body}", messageBody);
        long id = 0;
        try
        {
            var msg = JsonSerializer.Deserialize<BusMessage>(messageBody, Opts);
            id = msg?.Id ?? 0;
        }
        catch (Exception ex) { logger.LogError(ex, "DeadLetter: cannot deserialize"); }

        if (id == 0) return;

        try
        {
            var client = httpFactory.CreateClient("WebApi");
            var update = new StatusUpdate("DeadLettered", "Message exhausted all retry attempts");
            await client.PutAsJsonAsync($"api/messages/{id}/status", update, Opts);
            logger.LogInformation("Marked {Id} as DeadLettered", id);
        }
        catch (Exception ex) { logger.LogError(ex, "DeadLetter: failed to update {Id}", id); }
    }
}
