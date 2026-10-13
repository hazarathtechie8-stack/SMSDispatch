using Microsoft.EntityFrameworkCore;

namespace SmsDispatch.Api.Data;

public class SmsDbContext(DbContextOptions<SmsDbContext> options) : DbContext(options)
{
    public DbSet<MessageEntity> Messages => Set<MessageEntity>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        mb.Entity<MessageEntity>(e =>
        {
            e.HasIndex(m => m.Status);
            e.HasIndex(m => m.CreatedDateTime);

            // Seed data
            var now = new DateTime(2025, 1, 15, 10, 0, 0, DateTimeKind.Utc);
            e.HasData(
                new MessageEntity { Id=1, To=15551234567, From=15559876543, Message="Hello Alice, your appointment is confirmed.", Status="Sent",    CreatedDateTime=now,              ModifiedDatetime=now.AddSeconds(4), RecipientName="Alice" },
                new MessageEntity { Id=2, To=15552345678, From=15559876543, Message="Hi Bob, reminder: meeting at 3pm.",             Status="Sent",    CreatedDateTime=now.AddMinutes(1), ModifiedDatetime=now.AddMinutes(1).AddSeconds(5), RecipientName="Bob" },
                new MessageEntity { Id=3, To=15553456789, From=15559876543, Message="Carol, your package has been delivered.",       Status="Failed",  CreatedDateTime=now.AddMinutes(2), ModifiedDatetime=now.AddMinutes(2).AddSeconds(3), ErrorMessage="Not Sent – Not a valid phone", RecipientName="Carol" },
                new MessageEntity { Id=4, To=15554567890, From=15559876543, Message="David, your order is ready for pickup.",       Status="Pending", CreatedDateTime=now.AddMinutes(3), ModifiedDatetime=now.AddMinutes(3) },
                new MessageEntity { Id=5, To=15555678901, From=15559876543, Message="Eve, your verification code is 847291.",       Status="Queued",  CreatedDateTime=now.AddMinutes(4), ModifiedDatetime=now.AddMinutes(4) }
            );
        });
    }
}
