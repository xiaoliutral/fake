using Fake.EntityFrameworkCore;
using Fake.EventBus.Distributed;
using Fake.EventBus.Messaging.Tests.Events;
using Fake.Messaging;
using Fake.Messaging.EntityFrameworkCore;
using Fake.Testing;
using Fake.UnitOfWork;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Fake.EventBus.Messaging.Tests;

public class OutboxEventLogServiceTests : ApplicationTestWithTools<FakeEventBusMessagingTestModule>
{
    protected override void SetApplicationCreationOptions(FakeApplicationCreationOptions options)
    {
        options.UseAutofac();
    }

    [Fact]
    public async Task 无环境事务时Enqueue扩展应抛错()
    {
        var store = ServiceProvider.GetRequiredService<IMessageOutboxStore>();
        var resolver = ServiceProvider.GetRequiredService<IEventRoutingKeyResolver>();

        await Should.ThrowAsync<InvalidOperationException>(async () =>
            await store.EnqueueIntegrationEventAsync(new TestOutboxIntegrationEvent(), resolver));
    }

    [Fact]
    public async Task 同DbTransaction写入后可按事务Id拉取()
    {
        Guid txId;
        var evt = new TestOutboxIntegrationEvent { Payload = "by-tx" };

        await using (var scope = ServiceProvider.CreateAsyncScope())
        {
            var ctx = scope.ServiceProvider.GetRequiredService<MessagingDbContext>();
            await using var tx = await ctx.Database.BeginTransactionAsync();
            txId = tx.TransactionId;
            await TestMessagingHelper.EnqueueAsync(scope.ServiceProvider, evt, ctx);
            await tx.CommitAsync();
        }

        await using (var scope = ServiceProvider.CreateAsyncScope())
        {
            var store = scope.ServiceProvider.GetRequiredService<IMessageOutboxStore>();
            var pending = await store.GetPendingByTransactionAsync(txId.ToString());
            pending.Count.ShouldBe(1);
            pending[0].MessageId.ShouldBe(evt.Id);
            pending[0].State.ShouldBe(MessageOutboxState.NotPublished);
            pending[0].Kind.ShouldBe(MessageKind.EventBus);
        }
    }

    [Fact]
    public async Task 全局扫描应包含NotPublished与PublishFailed()
    {
        Guid failedEventId;

        await using (var scope = ServiceProvider.CreateAsyncScope())
        {
            var ctx = scope.ServiceProvider.GetRequiredService<MessagingDbContext>();
            var store = scope.ServiceProvider.GetRequiredService<IMessageOutboxStore>();
            await using var tx = await ctx.Database.BeginTransactionAsync();
            await TestMessagingHelper.EnqueueAsync(
                scope.ServiceProvider, new TestOutboxIntegrationEvent { Payload = "a" }, ctx);
            var failed = new TestOutboxIntegrationEvent { Payload = "b" };
            failedEventId = failed.Id;
            await TestMessagingHelper.EnqueueAsync(scope.ServiceProvider, failed, ctx);
            await tx.CommitAsync();
        }

        await using (var scope = ServiceProvider.CreateAsyncScope())
        {
            var store = scope.ServiceProvider.GetRequiredService<IMessageOutboxStore>();
            var batch = await TestMessagingHelper.GetPendingEventBusAsync(scope.ServiceProvider, 10);
            batch.Count.ShouldBeGreaterThanOrEqualTo(2);

            (await store.TryMarkInProgressAsync(failedEventId, TimeSpan.FromMinutes(1))).ShouldBeTrue();
            await store.MarkPublishFailedAsync(failedEventId, "fail");

            var afterFail = await TestMessagingHelper.GetPendingEventBusAsync(scope.ServiceProvider, 10);
            afterFail.Any(e => e.MessageId == failedEventId && e.State == MessageOutboxState.PublishFailed)
                .ShouldBeTrue();
        }
    }

    [Fact]
    public async Task EfCoreTransactionApi应实现IOutboxEnlistableTransaction()
    {
        var uowManager = ServiceProvider.GetRequiredService<IUnitOfWorkManager>();
        using var uow = uowManager.Begin(OutboxTestHelper.TransactionalUow());

        var dbProvider = ServiceProvider.GetRequiredService<IEfDbContextProvider<MessagingDbContext>>();
        await dbProvider.GetDbContextAsync();

        var enlistable = uow.GetAllActiveTransactionApis().OfType<IOutboxEnlistableTransaction>().Single();
        enlistable.TransactionId.ShouldNotBe(Guid.Empty);
        enlistable.GetDbTransaction().ShouldNotBeNull();

        await uow.CompleteAsync();
    }
}
