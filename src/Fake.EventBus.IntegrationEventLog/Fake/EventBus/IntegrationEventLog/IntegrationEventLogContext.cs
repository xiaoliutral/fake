using Fake.EntityFrameworkCore;
using Fake.EventBus.Distributed;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fake.EventBus.IntegrationEventLog;

public class IntegrationEventLogContext : EfCoreDbContext<IntegrationEventLogContext>
{
    public DbSet<OutboxEventLogEntry> OutboxEventLogs { get; set; } = null!;
    public DbSet<InboxEventLogEntry> InboxEventLogs { get; set; } = null!;

    public IntegrationEventLogContext(DbContextOptions<IntegrationEventLogContext> options) : base(options)
    {
        System.Diagnostics.Debug.WriteLine("IntegrationEventLogContext::ctor ->" + base.GetHashCode());
    }


    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<OutboxEventLogEntry>(ConfigureOutboxEventLogEntry);
        builder.Entity<InboxEventLogEntry>(ConfigureInboxEventLogEntry);
    }

    void ConfigureOutboxEventLogEntry(EntityTypeBuilder<OutboxEventLogEntry> builder)
    {
        builder.ToTable("OutboxEventLog");

        builder.HasKey(e => e.EventId);

        builder.Property(e => e.EventId)
            .IsRequired();

        builder.Property(e => e.Content)
            .HasMaxLength(-1)
            .IsRequired();

        builder.Property(e => e.CreationTime)
            .IsRequired();

        builder.Property(e => e.State)
            .IsRequired();

        builder.Property(e => e.TimesSent)
            .IsRequired();

        builder.Property(e => e.EventTypeName)
            .HasMaxLength(512)
            .IsRequired();

        // WHERE State IN (NotPublished, PublishFailed) ORDER BY CreationTime
        builder.HasIndex(e => new { e.State, e.CreationTime })
            .HasDatabaseName("IX_OutboxEventLog_State_CreationTime");

        builder.HasIndex(e => e.TransactionId)
            .HasDatabaseName("IX_OutboxEventLog_TransactionId");
    }

    void ConfigureInboxEventLogEntry(EntityTypeBuilder<InboxEventLogEntry> builder)
    {
        builder.ToTable("InboxEventLog");

        builder.HasKey(e => e.EventId);

        builder.Property(e => e.EventId)
            .IsRequired();

        builder.Property(e => e.EventTypeName)
            .HasMaxLength(512)
            .IsRequired();

        builder.Property(e => e.Content)
            .HasMaxLength(-1)
            .IsRequired();

        builder.Property(e => e.ProcessedTime)
            .IsRequired();

        builder.Property(e => e.State)
            .IsRequired();

        // 添加索引以提高查询性能
        builder.HasIndex(e => e.ProcessedTime);
    }
}