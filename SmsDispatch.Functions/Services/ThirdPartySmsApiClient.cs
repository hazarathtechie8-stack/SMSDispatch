using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace SmsDispatch.Functions.Services;

public class ThirdPartySmsApiClient(HttpClient http, ILogger<ThirdPartySmsApiClient> logger)
    : ISmsApiClient
{
    private static readonly JsonSerializerOptions Opts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async Task<string> SendAsync(string toPhone, string fromPhone, string message,
                                        CancellationToken ct = default)
    {
        var payload  = new { to = toPhone, from = fromPhone, body = message };
        var response = await http.PostAsJsonAsync("send", payload, Opts, ct);

        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadAsStringAsync(ct);
        logger.LogDebug("SMS API raw response: {Result}", result);
        return result.Trim();
    }
}
