using Fake.EventBus.Distributed;
using Fake.EventBus.IntegrationEventLog.Tests.Events;
using Fake.Testing;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Fake.EventBus.IntegrationEventLog.Tests;

public class AdminQueryFilterTests : ApplicationTestWithTools<FakeEventBusIntegrationEventLogTestModule>
{
    protected override void SetApplicationCreationOptions(FakeApplicationCreationOptions options)
    {
        options.UseAutofac();
    }

    [Fact]
    public async Task Outbox_支持时间范围与消息体过滤()
    {
        Guid matchId, otherId;
        await using (var scope = ServiceProvider.CreateAsyncScope())
        {
            var ctx = scope.ServiceProvider.GetRequiredService<IntegrationEventLogContext>();
            var outbox = scope.ServiceProvider.GetRequiredService<IOutboxEventLogService>();
            await using var tx = await ctx.Database.BeginTransactionAsync();

            var match = new TestOutboxIntegrationEvent { Payload = "needle-alpha" };
            var other = new TestOutboxIntegrationEvent { Payload = "other-beta" };
            matchId = match.Id;
            otherId = other.Id;
            await outbox.SaveEventAsync(match, ctx);
            await outbox.SaveEventAsync(other, ctx);
            await tx.CommitAsync();
        }

        await using (var scope = ServiceProvider.CreateAsyncScope())
        {
            var admin = scope.ServiceProvider.GetRequiredService<IOutboxAdminStore>();
            var byBody = await admin.QueryAsync(new OutboxQueryRequest
            {
                Content = "needle-alpha",
                PageSize = 50
            });
            byBody.Items.ShouldContain(x => x.EventId == matchId);
            byBody.Items.ShouldNotContain(x => x.EventId == otherId);

            var byRange = await admin.QueryAsync(new OutboxQueryRequest
            {
                From = DateTime.UtcNow.AddHours(-1),
                To = DateTime.UtcNow.AddHours(1),
                PageSize = 50
            });
            byRange.Items.ShouldContain(x => x.EventId == matchId);
            byRange.Items.ShouldContain(x => x.EventId == otherId);

            var byId = await admin.QueryAsync(new OutboxQueryRequest
            {
                EventId = matchId
            });
            byId.Total.ShouldBe(1);
            byId.Items[0].EventId.ShouldBe(matchId);
        }
    }

    [Fact]
    public async Task Inbox_支持时间范围与消息体错误过滤()
    {
        var match = new TestOutboxIntegrationEvent { Payload = "inbox-needle" };
        var other = new TestOutboxIntegrationEvent { Payload = "inbox-other" };
        var jsonMatch = System.Text.Json.JsonSerializer.Serialize(match);
        var jsonOther = System.Text.Json.JsonSerializer.Serialize(other);

        await using (var scope = ServiceProvider.CreateAsyncScope())
        {
            var inbox = scope.ServiceProvider.GetRequiredService<IInboxEventLogService>();
            (await inbox.TryMarkAsProcessingAsync(match.Id, typeof(TestOutboxIntegrationEvent).FullName!, jsonMatch))
                .ShouldBeTrue();
            await inbox.MarkAsFailedAsync(match.Id, "boom-timeout");

            (await inbox.TryMarkAsProcessingAsync(other.Id, typeof(TestOutboxIntegrationEvent).FullName!, jsonOther))
                .ShouldBeTrue();
            await inbox.MarkAsSucceededAsync(other.Id);
        }

        await using (var scope = ServiceProvider.CreateAsyncScope())
        {
            var admin = scope.ServiceProvider.GetRequiredService<IInboxAdminStore>();

            var byBody = await admin.QueryAsync(new InboxQueryRequest { Content = "inbox-needle" });
            byBody.Items.ShouldContain(x => x.EventId == match.Id);
            byBody.Items.ShouldNotContain(x => x.EventId == other.Id);

            var byError = await admin.QueryAsync(new InboxQueryRequest { Error = "boom-timeout" });
            byError.Items.ShouldContain(x => x.EventId == match.Id);

            var byRange = await admin.QueryAsync(new InboxQueryRequest
            {
                From = DateTime.UtcNow.AddHours(-1),
                To = DateTime.UtcNow.AddHours(1),
                State = EventState.ConsumeFailed
            });
            byRange.Items.ShouldContain(x => x.EventId == match.Id);
        }
    }
}
