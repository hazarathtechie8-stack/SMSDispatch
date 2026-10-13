using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmsDispatch.Api.Data;

[Table("Messages")]
public class MessageEntity
{
    [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column(TypeName = "numeric(18,0)")]
    public decimal Id { get; set; }

    [Column(TypeName = "numeric(18,0)")]
    public decimal To { get; set; }

    [Column(TypeName = "numeric(18,0)")]
    public decimal From { get; set; }

    [MaxLength(1000)]
    public string Message { get; set; } = string.Empty;

    [MaxLength(100)]
    public string Status { get; set; } = "Pending";

    public DateTime CreatedDateTime  { get; set; }
    public DateTime ModifiedDatetime { get; set; }

    // Extended fields (not in source schema — nullable so migration is additive)
    [MaxLength(200)]
    public string? RecipientName  { get; set; }

    [MaxLength(500)]
    public string? ErrorMessage   { get; set; }

    [MaxLength(50)]
    public string? ExternalSid    { get; set; }
}
