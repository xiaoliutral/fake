using Fake.Data;
using Fake.EntityFrameworkCore;
using Fake.EntityFrameworkCore.Modeling;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fake.Messaging.EntityFrameworkCore;

[ConnectionStringName(MessagingDbContextServiceCollectionExtensions.DefaultConnectionStringName)]
public class MessagingDbContext : EfCoreDbContext<MessagingDbContext>
{
    public DbSet<MessageOutboxEntry> OutboxMessages { get; set; } = null!;
    public DbSet<MessageInboxEntry> InboxMessages { get; set; } = null!;

    public MessagingDbContext(DbContextOptions<MessagingDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.Entity<MessageOutboxEntry>(ConfigureOutbox);
        builder.Entity<MessageInboxEntry>(ConfigureInbox);
    }

    private static void ConfigureOutbox(EntityTypeBuilder<MessageOutboxEntry> builder)
    {
        builder.ToFakeTable("msg_outbox");
        builder.HasKey(e => e.MessageId);

        builder.Property(e => e.Destination).HasMaxLength(512).IsRequired();
        builder.Property(e => e.Kind).IsRequired();
        builder.Property(e => e.Body).HasMaxLength(-1).IsRequired();
        builder.Property(e => e.TypeName).HasMaxLength(512);
        builder.Property(e => e.TransactionId).HasMaxLength(64).IsRequired();
        builder.Property(e => e.HeadersJson).HasMaxLength(-1).IsRequired();
        builder.Property(e => e.State).IsRequired();
        builder.Property(e => e.TimesSent).IsRequired();
        builder.Property(e => e.CreationTime).IsRequired();
        builder.Property(e => e.LastError).HasMaxLength(-1);

        builder.HasIndex(e => new { e.State, e.CreationTime })
            .HasDatabaseName("ix_" + FakeDbProperties.GetTableName("msg_outbox") + "_state_creation_time");
        builder.HasIndex(e => e.TransactionId)
            .HasDatabaseName("ix_" + FakeDbProperties.GetTableName("msg_outbox") + "_transaction_id");
        builder.HasIndex(e => e.Kind)
            .HasDatabaseName("ix_" + FakeDbProperties.GetTableName("msg_outbox") + "_kind");
    }

    private static void ConfigureInbox(EntityTypeBuilder<MessageInboxEntry> builder)
    {
        builder.ToFakeTable("msg_inbox");
        // 幂等键：(MessageId, ConsumerGroup)
        builder.HasKey(e => new { e.MessageId, e.ConsumerGroup });

        builder.Property(e => e.ConsumerGroup).HasMaxLength(256).IsRequired();
        builder.Property(e => e.Kind).IsRequired();
        builder.Property(e => e.Body).HasMaxLength(-1).IsRequired();
        builder.Property(e => e.TypeName).HasMaxLength(512);
        builder.Property(e => e.Destination).HasMaxLength(512);
        builder.Property(e => e.HeadersJson).HasMaxLength(-1).IsRequired();
        builder.Property(e => e.State).IsRequired();
        builder.Property(e => e.ProcessedTime).IsRequired();
        builder.Property(e => e.ErrorMessage).HasMaxLength(-1);

        builder.HasIndex(e => e.ProcessedTime)
            .HasDatabaseName("ix_" + FakeDbProperties.GetTableName("msg_inbox") + "_processed_time");
        builder.HasIndex(e => e.Kind)
            .HasDatabaseName("ix_" + FakeDbProperties.GetTableName("msg_inbox") + "_kind");
    }
}
