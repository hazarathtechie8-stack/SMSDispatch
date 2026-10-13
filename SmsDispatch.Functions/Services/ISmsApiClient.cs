namespace SmsDispatch.Functions.Services;

public interface ISmsApiClient
{
    /// <summary>
    /// Sends an SMS via the third-party API.
    /// Returns the raw result string: "Successfully Sent",
    /// "Not Sent – Not a valid phone", "Not Sent – Not valid by Time zone", etc.
    /// </summary>
    Task<string> SendAsync(string toPhone, string fromPhone, string message,
                           CancellationToken ct = default);
}
