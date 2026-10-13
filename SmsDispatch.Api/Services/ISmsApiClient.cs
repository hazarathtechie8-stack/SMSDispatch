namespace SmsDispatch.Api.Services;

/// <summary>Stub interface for the third-party SMS delivery API.</summary>
public interface ISmsApiClient
{
    /// <summary>Returns "Successfully Sent", "Not Sent – Not a valid phone", or "Not Sent – Not valid by Time zone".</summary>
    Task<string> SendAsync(string to, string from, string message, CancellationToken ct = default);
}

/// <summary>
/// Real implementation — replace URL/auth with actual third-party credentials.
/// Registered in Functions project; NOT used by Web API directly.
/// </summary>
public class ThirdPartySmsApiClient(HttpClient http) : ISmsApiClient
{
    public async Task<string> SendAsync(string to, string from, string message, CancellationToken ct = default)
    {
        var payload  = new { To = to, From = from, Message = message };
        var response = await http.PostAsJsonAsync("send", payload, ct);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<SmsApiResult>(cancellationToken: ct);
        return result?.Status ?? "Not Sent – Not a valid phone";
    }

    private record SmsApiResult(string Status);
}
