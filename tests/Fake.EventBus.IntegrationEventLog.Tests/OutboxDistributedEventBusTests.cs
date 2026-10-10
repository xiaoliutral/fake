using Fake.EntityFrameworkCore;
using Fake.EventBus.Distributed;
using Fake.EventBus.IntegrationEventLog.Tests.Events;
using Fake.EventBus.IntegrationEventLog.Tests.Fakes;
using Fake.Testing;
using Fake.UnitOfWork;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Fake.EventBus.IntegrationEventLog.Tests;

public class OutboxDistributedEventBusTests : ApplicationTestWithTools<FakeEventBusIntegrationEventLogTestModule>
{
    private readonly IUnitOfWorkManager _uowManager;
    private readonly IDistributedEventBus _distributedEventBus;
    private readonly FakePhysicalDistributedEventBus _physicalEventBus;
    private readonly IOutboxEventLogService _outbox;
    private readonly IEfDbContextProvider<IntegrationEventLogContext> _dbProvider;

    protected override void SetApplicationCreationOptions(FakeApplicationCreationOptions options)
    {
        options.UseAutofac();
    }

    public OutboxDistributedEventBusTests()
    {
        _uowManager = ServiceProvider.GetRequiredService<IUnitOfWorkManager>();
        _distributedEventBus = ServiceProvider.GetRequiredService<IDistributedEventBus>();
        _physicalEventBus = ServiceProvider.GetRequiredService<FakePhysicalDistributedEventBus>();
        _outbox = ServiceProvider.GetRequiredService<IOutboxEventLogService>();
        _dbProvider = ServiceProvider.GetRequiredService<IEfDbContextProvider<IntegrationEventLogContext>>();
    }

    [Fact]
    public void IDistributedEventBus应为Outbox装饰器()
    {
        _distributedEventBus.ShouldBeOfType<OutboxDistributedEventBus>();
    }

    [Fact]
    public async Task 无工作单元发布应抛错()
    {
        _uowManager.Current.ShouldBeNull();

        var ex = await Should.ThrowAsync<InvalidOperationException>(() =>
            _distributedEventBus.PublishAsync(new TestOutboxIntegrationEvent()));

        ex.Message.ShouldContain("事务性工作单元");
        _physicalEventBus.Published.Count.ShouldBe(0);
    }

    [Fact]
    public async Task 事务Uow内发布应落库并在Complete后投递物理总线()
    {
        _physicalEventBus.Clear();
        var evt = new TestOutboxIntegrationEvent { Payload = "uow-flow" };

        using (var uow = _uowManager.Begin(OutboxTestHelper.TransactionalUow()))
        {
            await _dbProvider.GetDbContextAsync();
            await _distributedEventBus.PublishAsync(evt);
            _physicalEventBus.Published.Count.ShouldBe(0);
            await uow.CompleteAsync();
        }

        _physicalEventBus.Published.Count.ShouldBe(1);
        _physicalEventBus.Published[0].Id.ShouldBe(evt.Id);
        _physicalEventBus.Published[0].ShouldBeOfType<TestOutboxIntegrationEvent>()
            .Payload.ShouldBe("uow-flow");

        var pending = (await _outbox.RetrieveEventLogsPendingToPublishAsync(50)).ToList();
        pending.Any(e => e.EventId == evt.Id).ShouldBeFalse();
    }

    [Fact]
    public async Task 先发布后开库应在SaveChanges时刷入Outbox()
    {
        _physicalEventBus.Clear();
        var evt = new TestOutboxIntegrationEvent { Payload = "queued-then-flush" };

        using (var uow = _uowManager.Begin(OutboxTestHelper.TransactionalUow()))
        {
            await _distributedEventBus.PublishAsync(evt);
            _physicalEventBus.Published.Count.ShouldBe(0);

            await _dbProvider.GetDbContextAsync();
            await uow.CompleteAsync();
        }

        _physicalEventBus.Published.Count.ShouldBe(1);
        _physicalEventBus.Published[0].Id.ShouldBe(evt.Id);
        _physicalEventBus.Published[0].ShouldBeOfType<TestOutboxIntegrationEvent>()
            .Payload.ShouldBe("queued-then-flush");
    }

    [Fact]
    public async Task 回滚后不应投递且Outbox无已提交行()
    {
        _physicalEventBus.Clear();
        var evt = new TestOutboxIntegrationEvent { Payload = "rollback" };

        using (var uow = _uowManager.Begin(OutboxTestHelper.TransactionalUow()))
        {
            await _dbProvider.GetDbContextAsync();
            await _distributedEventBus.PublishAsync(evt);
            await uow.RollbackAsync();
        }

        _physicalEventBus.Published.Count.ShouldBe(0);

        await using var scope = ServiceProvider.CreateAsyncScope();
        var outbox = scope.ServiceProvider.GetRequiredService<IOutboxEventLogService>();
        var pending = (await outbox.RetrieveEventLogsPendingToPublishAsync(50)).ToList();
        pending.Any(e => e.EventId == evt.Id).ShouldBeFalse();
    }
}
