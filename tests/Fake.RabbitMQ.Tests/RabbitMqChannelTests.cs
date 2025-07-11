
using Fake.RabbitMQ;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

public class RabbitMqChannelTests: ApplicationTestBase<FakeRabbitMqTestModule>
{
    private IRabbitMqChannelPool _rabbitMqChannelPool;
    public RabbitMqChannelTests()
    {
        _rabbitMqChannelPool = ServiceProvider.GetRequiredService<IRabbitMqChannelPool>();
    }

    [Fact]
    public Task 通道可用()
    {
        using var channelAccessor = _rabbitMqChannelPool.Acquire("test");
        
        var properties = channelAccessor.Channel.CreateBasicProperties();
        properties.DeliveryMode = 2; // Non-persistent (1) or persistent (2).
        
        channelAccessor.Channel.BasicPublish("test", "test", true,  properties, ReadOnlyMemory<byte>.Empty);
        
        return Task.CompletedTask;
    }
    
    
    [Fact]
    public async Task 线程安全()
    {
        var tasks = new List<Task>();
        for (int i = 0; i < 100; i++)
        {
            tasks.Add(PublishMessage());
        }
        
        await Task.WhenAll(tasks);

        Task PublishMessage()
        {
            using var channelAccessor = _rabbitMqChannelPool.Acquire("test");
        
            var properties = channelAccessor.Channel.CreateBasicProperties();
            properties.DeliveryMode = 2; // Non-persistent (1) or persistent (2).
        
            channelAccessor.Channel.BasicPublish("test", "test", true,  properties, ReadOnlyMemory<byte>.Empty);
            
            return Task.CompletedTask;
        }
    }
}