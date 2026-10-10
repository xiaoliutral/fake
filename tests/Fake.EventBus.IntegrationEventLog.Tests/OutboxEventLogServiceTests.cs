using Fake.EntityFrameworkCore;
using Fake.EventBus.Distributed;
using Fake.EventBus.IntegrationEventLog.Tests.Events;
using Fake.Testing;
using Fake.UnitOfWork;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Fake.EventBus.IntegrationEventLog.Tests;

public class OutboxEventLogServiceTests : ApplicationTestWithTools<FakeEventBusIntegrationEventLogTestModule>
{
    protected override void SetApplicationCreationOptions(FakeApplicationCreationOptions options)
    {
        options.UseAutofac();
    }

    [Fact]
    public async Task 无环境事务时SaveEvent扩展应抛错()
    {
        var outbox = ServiceProvider.GetRequiredService<IOutboxEventLogService>();

        await Should.ThrowAsync<InvalidOperationException>(async () =>
            await outbox.SaveEventAsync(new TestOutboxIntegrationEvent()));
    }

    [Fact]
    public async Task 同DbTransaction写入后可按事务Id拉取()
    {
        Guid txId;
        var evt = new TestOutboxIntegrationEvent { Payload = "by-tx" };

        await using (var scope = ServiceProvider.CreateAsyncScope())
        {
            var ctx = scope.ServiceProvider.GetRequiredService<IntegrationEventLogContext>();
            var outbox = scope.ServiceProvider.GetRequiredService<IOutboxEventLogService>();

            await using var tx = await ctx.Database.BeginTransactionAsync();
            txId = tx.TransactionId;

            // 同一 Scoped Context：扩展方法加入当前事务
            await outbox.SaveEventAsync(evt, ctx);
            await tx.CommitAsync();
        }

        await using (var scope = ServiceProvider.CreateAsyncScope())
        {
            var outbox = scope.ServiceProvider.GetRequiredService<IOutboxEventLogService>();
            var pending = (await outbox.RetrieveEventLogsPendingToPublishAsync(txId)).ToList();
            pending.Count.ShouldBe(1);
            pending[0].EventId.ShouldBe(evt.Id);
            pending[0].State.ShouldBe(EventState.NotPublished);
            pending[0].IntegrationEvent.ShouldBeOfType<TestOutboxIntegrationEvent>();
        }
    }

    [Fact]
    public async Task 全局扫描应包含NotPublished与PublishFailed()
    {
        Guid failedEventId;

        await using (var scope = ServiceProvider.CreateAsyncScope())
        {
            var ctx = scope.ServiceProvider.GetRequiredService<IntegrationEventLogContext>();
            var outbox = scope.ServiceProvider.GetRequiredService<IOutboxEventLogService>();

            await using var tx = await ctx.Database.BeginTransactionAsync();
            await outbox.SaveEventAsync(new TestOutboxIntegrationEvent { Payload = "a" }, ctx);
            var failed = new TestOutboxIntegrationEvent { Payload = "b" };
            failedEventId = failed.Id;
            await outbox.SaveEventAsync(failed, ctx);
            await tx.CommitAsync();
        }

        await using (var scope = ServiceProvider.CreateAsyncScope())
        {
            var outbox = scope.ServiceProvider.GetRequiredService<IOutboxEventLogService>();

            var batch = (await outbox.RetrieveEventLogsPendingToPublishAsync(10)).ToList();
            batch.Count.ShouldBeGreaterThanOrEqualTo(2);

            (await outbox.TryMarkEventAsInProgressAsync(failedEventId, TimeSpan.FromMinutes(1))).ShouldBeTrue();
            await outbox.MarkEventAsFailedAsync(failedEventId);

            var afterFail = (await outbox.RetrieveEventLogsPendingToPublishAsync(10)).ToList();
            afterFail.Any(e => e.EventId == failedEventId && e.State == EventState.PublishFailed).ShouldBeTrue();
        }
    }

    [Fact]
    public async Task EfCoreTransactionApi应实现IOutboxEnlistableTransaction()
    {
        var uowManager = ServiceProvider.GetRequiredService<IUnitOfWorkManager>();
        using var uow = uowManager.Begin(OutboxTestHelper.TransactionalUow());

        var dbProvider = ServiceProvider.GetRequiredService<IEfDbContextProvider<IntegrationEventLogContext>>();
        await dbProvider.GetDbContextAsync();

        var enlistable = uow.GetAllActiveTransactionApis().OfType<IOutboxEnlistableTransaction>().Single();
        enlistable.TransactionId.ShouldNotBe(Guid.Empty);
        enlistable.GetDbTransaction().ShouldNotBeNull();

        await uow.CompleteAsync();
    }
}
