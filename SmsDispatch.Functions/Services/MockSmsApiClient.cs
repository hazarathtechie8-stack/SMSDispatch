using Microsoft.Extensions.Logging;

namespace SmsDispatch.Functions.Services;

public class MockSmsApiClient(ILogger<MockSmsApiClient> logger) : ISmsApiClient
{
    public Task<string> SendAsync(string toPhone, string fromPhone, string message,
                                  CancellationToken ct = default)
    {
        logger.LogInformation("MockSmsApiClient: pretending to send to {To} from {From}",
            toPhone, fromPhone);
        return Task.FromResult("Successfully Sent");
    }
}