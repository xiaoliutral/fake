using Fake.RabbitMQ;
using Fake.Timing;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using RabbitMQ.Client;
using Shouldly;
using Xunit;

public class RabbitMqChannelPoolTests
{
    [Fact]
    public void Release_应命中Acquire创建的通道并归还到池中()
    {
        var model = Substitute.For<IModel>();
        var connectionPool = Substitute.For<IRabbitMqConnectionPool>();
        var connection = Substitute.For<IConnection>();
        connection.CreateModel().Returns(model);
        connectionPool.Get(Arg.Any<string?>()).Returns(connection);

        var pool = CreatePool(connectionPool, TimeSpan.FromMilliseconds(50));

        var accessor = pool.Acquire("test");

        pool.Release("test").ShouldBeTrue();
        model.DidNotReceive().Dispose();

        accessor.Dispose();

        var nextAccessor = pool.Acquire("test");
        nextAccessor.IsNew.ShouldBeFalse();
        ReferenceEquals(nextAccessor.Channel, model).ShouldBeTrue();
        connection.Received(1).CreateModel();

        nextAccessor.Dispose();
    }

    [Fact]
    public void Release_在通道使用中时不应关闭底层通道()
    {
        var model = Substitute.For<IModel>();
        var pool = CreatePool(TimeSpan.FromMilliseconds(50), model);

        var accessor = pool.Acquire("test");

        pool.Release("test").ShouldBeTrue();
        model.DidNotReceive().Dispose();

        accessor.Dispose();

        model.DidNotReceive().Dispose();
        pool.Release("test").ShouldBeFalse();
    }

    [Fact]
    public void Dispose_存在空闲通道时不应抛异常并应释放底层通道()
    {
        var model1 = Substitute.For<IModel>();
        var model2 = Substitute.For<IModel>();
        var pool = CreatePool(TimeSpan.FromMilliseconds(50), model1, model2);

        using (pool.Acquire("first"))
        {
        }

        using (pool.Acquire("second"))
        {
        }

        var exception = Record.Exception(pool.Dispose);

        exception.ShouldBeNull();
        model1.Received(1).Dispose();
        model2.Received(1).Dispose();
    }

    [Fact]
    public void Dispose_通道使用超时后不应强制释放_应在租约结束后释放底层通道()
    {
        var model = Substitute.For<IModel>();
        var pool = CreatePool(TimeSpan.FromMilliseconds(50), model);

        var accessor = pool.Acquire("test");

        var exception = Record.Exception(pool.Dispose);

        exception.ShouldBeNull();
        model.DidNotReceive().Dispose();

        accessor.Dispose();

        model.Received(1).Dispose();
    }

    private static RabbitMqChannelPool CreatePool(TimeSpan disposeDuration, params IModel[] models)
    {
        var connectionPool = Substitute.For<IRabbitMqConnectionPool>();
        var connection = Substitute.For<IConnection>();
        var index = 0;
        connection.CreateModel().Returns(_ =>
        {
            var currentIndex = Math.Min(index, models.Length - 1);
            index++;
            return models[currentIndex];
        });
        connectionPool.Get(Arg.Any<string?>()).Returns(connection);

        return CreatePool(connectionPool, disposeDuration);
    }

    private static RabbitMqChannelPool CreatePool(IRabbitMqConnectionPool connectionPool, TimeSpan disposeDuration)
    {
        var clock = new Clock(Options.Create(new FakeClockOptions()));
        var options = Options.Create(new FakeRabbitMqOptions
        {
            ChannelPoolDisposeDuration = disposeDuration
        });

        return new RabbitMqChannelPool(
            connectionPool,
            NullLogger<RabbitMqChannelPool>.Instance,
            clock,
            options
        );
    }
}
