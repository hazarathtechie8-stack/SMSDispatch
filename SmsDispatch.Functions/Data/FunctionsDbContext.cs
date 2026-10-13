using Microsoft.EntityFrameworkCore;
using SmsDispatch.Functions.Models;

namespace SmsDispatch.Functions.Data;

/// <summary>
/// Minimal DbContext owned by Functions. Maps to the same dbo.Messages table
/// as the Web API but has zero dependency on SmsDispatch.Api.
/// </summary>
public class FunctionsDbContext(DbContextOptions<FunctionsDbContext> options)
    : DbContext(options)
{
    public DbSet<MessageRecord> Messages => Set<MessageRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MessageRecord>(e =>
        {
            e.ToTable("Messages");
            e.HasKey(m => m.Id);
            e.Property(m => m.Id).ValueGeneratedOnAdd().HasColumnType("decimal(20,0)");
            // Column names must match what the Web API migration created.
            // EF uses the C# property name as column name by default, so
            // "To" → column "To", "From" → column "From" unless overridden.
            // If your table has ToPhoneNumber/FromPhoneNumber columns, swap these back.
            e.Property(m => m.To).HasColumnName("To").HasColumnType("decimal(15,0)");
            e.Property(m => m.From).HasColumnName("From").HasColumnType("decimal(15,0)");
            e.Property(m => m.Message).HasMaxLength(160);
            e.Property(m => m.Status).HasMaxLength(20);
            e.Property(m => m.RecipientName).HasMaxLength(100);
        });
    }
}
