using Fake.EventBus.Messaging.Tests.Events;
using Fake.Messaging;
using Shouldly;

namespace Fake.EventBus.Messaging.Tests;

public class MessagingOptionsTests
{
    [Fact]
    public void ResolveProcessingLockTimeout_默认值()
    {
        var options = new MessagingOptions();
        options.ResolveProcessingLockTimeout(nameof(TestOutboxIntegrationEvent))
            .ShouldBe(TimeSpan.FromMinutes(5));
    }

    [Fact]
    public void ResolveProcessingLockTimeout_按短名覆盖()
    {
        var options = new MessagingOptions
        {
            ProcessingLockTimeout = TimeSpan.FromMinutes(5),
            ProcessingLockTimeouts =
            {
                [nameof(TestOutboxIntegrationEvent)] = TimeSpan.FromMinutes(20)
            }
        };

        options.ResolveProcessingLockTimeout(typeof(TestOutboxIntegrationEvent).FullName)
            .ShouldBe(TimeSpan.FromMinutes(20));
    }

    [Fact]
    public void ResolveProcessingLockTimeout_按FullName覆盖()
    {
        var fullName = typeof(TestOutboxIntegrationEvent).FullName!;
        var options = new MessagingOptions
        {
            ProcessingLockTimeouts =
            {
                [fullName] = TimeSpan.FromMinutes(30)
            }
        };

        options.ResolveProcessingLockTimeout(fullName)
            .ShouldBe(TimeSpan.FromMinutes(30));
    }
}
