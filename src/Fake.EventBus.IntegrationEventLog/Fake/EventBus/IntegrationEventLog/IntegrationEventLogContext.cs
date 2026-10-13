using Fake.EntityFrameworkCore;
using Fake.EntityFrameworkCore.Modeling;
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
        builder.ToFakeTable("event_outbox");

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

        builder.Property(e => e.TransactionId)
            .HasMaxLength(64)
            .IsRequired();

        // WHERE State IN (NotPublished, PublishFailed) ORDER BY CreationTime
        builder.HasIndex(e => new { e.State, e.CreationTime })
            .HasDatabaseName("ix_" + FakeDbProperties.GetTableName("event_outbox") + "_state_creation_time");

        builder.HasIndex(e => e.TransactionId)
            .HasDatabaseName("ix_" + FakeDbProperties.GetTableName("event_outbox") + "_transaction_id");
    }

    void ConfigureInboxEventLogEntry(EntityTypeBuilder<InboxEventLogEntry> builder)
    {
        builder.ToFakeTable("event_inbox");

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

        builder.HasIndex(e => e.ProcessedTime)
            .HasDatabaseName("ix_" + FakeDbProperties.GetTableName("event_inbox") + "_processed_time");
    }
}

