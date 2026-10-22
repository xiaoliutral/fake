namespace Fake.Messaging.RabbitMQ;

public interface IRabbitMessageConsumer
{
    void Subscribe(RabbitConsumeOptions options);
}
