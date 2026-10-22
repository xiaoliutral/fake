using Fake.EntityFrameworkCore;
using Fake.EventBus.Distributed;
using Fake.EventBus.Messaging.Tests.Events;
using Fake.EventBus.Messaging.Tests.Fakes;
using Fake.Messaging.EntityFrameworkCore;
using Fake.Testing;
using Fake.UnitOfWork;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Fake.EventBus.Messaging.Tests;

public class OutboxDistributedEventBusTests : ApplicationTestWithTools<FakeEventBusMessagingTestModule>
{
    private readonly IUnitOfWorkManager _uowManager;
    private readonly IDistributedEventBus _distributedEventBus;
    private readonly FakePhysicalDistributedEventBus _physicalEventBus;
    private readonly IEfDbContextProvider<MessagingDbContext> _dbProvider;

    protected override void SetApplicationCreationOptions(FakeApplicationCreationOptions options)
    {
        options.UseAutofac();
    }

    public OutboxDistributedEventBusTests()
    {
        _uowManager = ServiceProvider.GetRequiredService<IUnitOfWorkManager>();
        _distributedEventBus = ServiceProvider.GetRequiredService<IDistributedEventBus>();
        _physicalEventBus = ServiceProvider.GetRequiredService<FakePhysicalDistributedEventBus>();
        _dbProvider = ServiceProvider.GetRequiredService<IEfDbContextProvider<MessagingDbContext>>();
    }

    [Fact]
    public void IDistributedEventBus应为Outbox装饰器()
    {
        _distributedEventBus.ShouldBeOfType<OutboxDistributedEventBus>();
    }

    [Fact]
    public async Task 无工作单元发布应立即走物理总线()
    {
        _uowManager.Current.ShouldBeNull();
        _physicalEventBus.Clear();
        var evt = new TestOutboxIntegrationEvent { Payload = "no-uow" };

        await _distributedEventBus.PublishAsync(evt);

        _physicalEventBus.Published.Count.ShouldBe(1);
        _physicalEventBus.Published[0].Id.ShouldBe(evt.Id);
    }

    [Fact]
    public async Task 非事务工作单元发布应立即走物理总线()
    {
        _physicalEventBus.Clear();
        var evt = new TestOutboxIntegrationEvent { Payload = "non-tx-uow" };

        using (var uow = _uowManager.Begin(new UnitOfWorkAttribute { IsTransactional = false }))
        {
            await _distributedEventBus.PublishAsync(evt);
            await uow.CompleteAsync();
        }

        _physicalEventBus.Published.Count.ShouldBe(1);
        _physicalEventBus.Published[0].Id.ShouldBe(evt.Id);
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

        var pending = await TestMessagingHelper.GetPendingEventBusAsync(ServiceProvider);
        pending.Any(e => e.MessageId == evt.Id).ShouldBeFalse();
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
        var pending = await TestMessagingHelper.GetPendingEventBusAsync(scope.ServiceProvider);
        pending.Any(e => e.MessageId == evt.Id).ShouldBeFalse();
    }
}
