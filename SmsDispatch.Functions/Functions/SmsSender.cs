using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Polly.CircuitBreaker;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using SmsDispatch.Functions.Models;
using SmsDispatch.Functions.Services;

namespace SmsDispatch.Functions.Functions;

/// <summary>
/// Service Bus trigger on the send-processor subscription.
/// Calls third-party SMS API with Polly resilience (retry + circuit breaker).
/// Updates Web API with final status.
/// </summary>
public class SmsSender(
    ISmsApiClient      httpSms,
    IHttpClientFactory httpFactory,
    ILogger<SmsSender> logger)
{
    private static readonly JsonSerializerOptions Opts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters           = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    private static readonly HashSet<string> PermanentFailures =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "Not Sent – Not a valid phone",
            "Not Sent – Not valid by Time zone"
        };

    [Function(nameof(SmsSender))]
    public async Task Run(
        [ServiceBusTrigger(
            topicName:        "sms-dispatch",
            subscriptionName: "send-processor",
            Connection = "ServiceBusConnection")]
        string messageBody,
        FunctionContext context)
    {
        BusMessage? msg;
        try
        {
            msg = JsonSerializer.Deserialize<BusMessage>(messageBody, Opts);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "SmsSender: cannot deserialise body; discarding");
            return;
        }

        if (msg is null) return;

        logger.LogInformation("SmsSender: processing {Id} to {To}", msg.Id, msg.ToPhoneNumber);

        await UpdateStatusAsync(msg.Id, "Sending", null, null);

        string apiResult;
        try
        {
            apiResult = await httpSms.SendAsync(
                msg.ToPhoneNumber,
                msg.FromPhoneNumber,
                msg.Message);
        }
        catch (BrokenCircuitException ex)
        {
            logger.LogError(ex, "SmsSender: SMS API circuit OPEN for {Id}; re-queuing", msg.Id);
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "SmsSender: retries exhausted for {Id}; abandoning", msg.Id);
            throw;
        }

        logger.LogInformation("SmsSender: {Id} API result = '{Result}'", msg.Id, apiResult);

        if (apiResult == "Successfully Sent")
        {
            await UpdateStatusAsync(msg.Id, "Sent", null, null);
        }
        else if (PermanentFailures.Contains(apiResult))
        {
            logger.LogWarning("SmsSender: permanent failure {Id}: {Result}", msg.Id, apiResult);
            await UpdateStatusAsync(msg.Id, "Failed", apiResult, null);
        }
        else
        {
            logger.LogWarning("SmsSender: unknown result '{Result}' for {Id}", apiResult, msg.Id);
            throw new InvalidOperationException(
                $"Unrecognised SMS API result: '{apiResult}' for message {msg.Id}");
        }
    }

    private async Task UpdateStatusAsync(long id, string status, string? error, string? sid)
    {
        try
        {
            var client   = httpFactory.CreateClient("WebApi");
            var update   = new StatusUpdate(status, error, sid);
            var response = await client.PutAsJsonAsync($"api/messages/{id}/status", update, Opts);

            if (!response.IsSuccessStatusCode)
                logger.LogWarning("SmsSender: status update {Id} returned {Code}", id, response.StatusCode);
        }
        catch (BrokenCircuitException ex)
        {
            logger.LogError(ex, "SmsSender: Web API circuit OPEN; skipping status for {Id}", id);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "SmsSender: status update failed for {Id}", id);
        }
    }
}
