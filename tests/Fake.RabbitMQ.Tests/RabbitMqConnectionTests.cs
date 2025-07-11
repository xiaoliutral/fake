using Fake.RabbitMQ;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

public class RabbitMqConnectionTests: ApplicationTestBase<FakeRabbitMqTestModule>
{
    private IRabbitMqConnectionPool _rabbitMqConnectionPool;
    public RabbitMqConnectionTests()
    {
        _rabbitMqConnectionPool = ServiceProvider.GetRequiredService<IRabbitMqConnectionPool>();
    }

    [Fact]
    public Task 可以连上()
    {
        var connection = _rabbitMqConnectionPool.Get();
        var channel = connection.CreateModel();
        
        var properties = channel.CreateBasicProperties();
        properties.DeliveryMode = 2; // Non-persistent (1) or persistent (2).
        channel.BasicPublish("test", "test", true,  properties, ReadOnlyMemory<byte>.Empty);
        return Task.CompletedTask;
    }
}