using Fake.EventBus.Messaging.Tests.Events;
using Fake.Messaging;
using Fake.Messaging.Admin;
using Fake.Messaging.EntityFrameworkCore;
using Fake.Testing;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Fake.EventBus.Messaging.Tests;

public class AdminQueryFilterTests : ApplicationTestWithTools<FakeEventBusMessagingTestModule>
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
            var ctx = scope.ServiceProvider.GetRequiredService<MessagingDbContext>();
            await using var tx = await ctx.Database.BeginTransactionAsync();

            var match = new TestOutboxIntegrationEvent { Payload = "needle-alpha" };
            var other = new TestOutboxIntegrationEvent { Payload = "other-beta" };
            matchId = match.Id;
            otherId = other.Id;
            await TestMessagingHelper.EnqueueAsync(scope.ServiceProvider, match, ctx);
            await TestMessagingHelper.EnqueueAsync(scope.ServiceProvider, other, ctx);
            await tx.CommitAsync();
        }

        await using (var scope = ServiceProvider.CreateAsyncScope())
        {
            var admin = scope.ServiceProvider.GetRequiredService<IMessageOutboxAdminStore>();
            var byBody = await admin.QueryAsync(new MessageOutboxQueryRequest
            {
                Content = "needle-alpha",
                PageSize = 50
            });
            byBody.Items.ShouldContain(x => x.MessageId == matchId);
            byBody.Items.ShouldNotContain(x => x.MessageId == otherId);

            var byRange = await admin.QueryAsync(new MessageOutboxQueryRequest
            {
                From = DateTime.UtcNow.AddHours(-1),
                To = DateTime.UtcNow.AddHours(1),
                PageSize = 50
            });
            byRange.Items.ShouldContain(x => x.MessageId == matchId);
            byRange.Items.ShouldContain(x => x.MessageId == otherId);

            var byId = await admin.QueryAsync(new MessageOutboxQueryRequest
            {
                MessageId = matchId
            });
            byId.Total.ShouldBe(1);
            byId.Items[0].MessageId.ShouldBe(matchId);

            var byKind = await admin.QueryAsync(new MessageOutboxQueryRequest
            {
                Kind = MessageKind.EventBus,
                PageSize = 50
            });
            byKind.Items.ShouldContain(x => x.MessageId == matchId);
        }
    }

    [Fact]
    public async Task Inbox_支持时间范围与消息体错误过滤()
    {
        var match = new TestOutboxIntegrationEvent { Payload = "inbox-needle" };
        var other = new TestOutboxIntegrationEvent { Payload = "inbox-other" };
        var jsonMatch = System.Text.Json.JsonSerializer.Serialize(match);
        var jsonOther = System.Text.Json.JsonSerializer.Serialize(other);
        var type = typeof(TestOutboxIntegrationEvent).FullName!;

        await using (var scope = ServiceProvider.CreateAsyncScope())
        {
            (await TestMessagingHelper.TryBeginInboxAsync(scope.ServiceProvider, match.Id, type, jsonMatch))
                .ShouldBeTrue();
            await TestMessagingHelper.MarkInboxFailedAsync(scope.ServiceProvider, match.Id, "boom-timeout");

            (await TestMessagingHelper.TryBeginInboxAsync(scope.ServiceProvider, other.Id, type, jsonOther))
                .ShouldBeTrue();
            await TestMessagingHelper.MarkInboxSucceededAsync(scope.ServiceProvider, other.Id);
        }

        await using (var scope = ServiceProvider.CreateAsyncScope())
        {
            var admin = scope.ServiceProvider.GetRequiredService<IMessageInboxAdminStore>();

            var byBody = await admin.QueryAsync(new MessageInboxQueryRequest { Content = "inbox-needle" });
            byBody.Items.ShouldContain(x => x.MessageId == match.Id);
            byBody.Items.ShouldNotContain(x => x.MessageId == other.Id);

            var byError = await admin.QueryAsync(new MessageInboxQueryRequest { Error = "boom-timeout" });
            byError.Items.ShouldContain(x => x.MessageId == match.Id);

            var byRange = await admin.QueryAsync(new MessageInboxQueryRequest
            {
                From = DateTime.UtcNow.AddHours(-1),
                To = DateTime.UtcNow.AddHours(1),
                State = MessageInboxState.ConsumeFailed
            });
            byRange.Items.ShouldContain(x => x.MessageId == match.Id);
        }
    }
}
