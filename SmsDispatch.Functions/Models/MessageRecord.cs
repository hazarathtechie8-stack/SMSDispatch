namespace SmsDispatch.Functions.Models;

/// <summary>
/// Lightweight EF entity owned by Functions — mirrors only the columns SmsPoller needs.
/// No reference to SmsDispatch.Api at all.
/// </summary>
public class MessageRecord
{
    public decimal  Id               { get; set; }   // DB column is numeric/decimal
    public decimal  To               { get; set; }
    public decimal  From             { get; set; }
    public string   Message          { get; set; } = "";
    public string?  RecipientName    { get; set; }
    public string   Status           { get; set; } = "Pending";
    public DateTime CreatedDateTime  { get; set; }
    public DateTime ModifiedDatetime { get; set; }

    /// <summary>Convenience cast for logging and Service Bus MessageId.</summary>
    public long IdAsLong => (long)Id;
}
