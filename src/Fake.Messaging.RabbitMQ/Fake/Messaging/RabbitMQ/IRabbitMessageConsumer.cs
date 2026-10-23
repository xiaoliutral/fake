namespace Fake.Messaging.RabbitMQ;

public interface IRabbitMessageConsumer
{
    void Subscribe(RabbitSubscription subscription);
}
