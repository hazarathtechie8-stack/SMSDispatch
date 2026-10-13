using Azure.Messaging.ServiceBus;
using System.Text.Json;
using System.Text.Json.Serialization;
using SmsDispatch.Api.Data;

namespace SmsDispatch.Api.Services;

public interface IServiceBusPublisher
{
    Task PublishAsync(MessageEntity entity, CancellationToken ct = default);
}

public class ServiceBusPublisher(ServiceBusSender sender, ILogger<ServiceBusPublisher> logger)
    : IServiceBusPublisher
{
    private static readonly JsonSerializerOptions Opts = new()
    {
        PropertyNamingPolicy   = JsonNamingPolicy.CamelCase,
        Converters             = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public async Task PublishAsync(MessageEntity entity, CancellationToken ct = default)
    {
        var payload = new
        {
            id              = (long)entity.Id,
            toPhoneNumber   = entity.To.ToString("F0"),
            fromPhoneNumber = entity.From.ToString("F0"),
            message         = entity.Message,
            recipientName   = entity.RecipientName,
            queuedAt        = entity.CreatedDateTime
        };

        var json  = JsonSerializer.Serialize(payload, Opts);
        var sbMsg = new ServiceBusMessage(json)
        {
            ContentType   = "application/json",
            MessageId     = ((long)entity.Id).ToString(),
            CorrelationId = ((long)entity.Id).ToString()
        };

        await sender.SendMessageAsync(sbMsg, ct);
        logger.LogInformation("Published message {Id} to Service Bus", (long)entity.Id);
    }
}
