namespace Fake.EventBus.RabbitMQ;

internal static class RabbitMqDlxNames
{
    public static string Exchange(string exchangeName) => "DLX." + exchangeName;

    public static string Queue(string queueName) => "DLX." + queueName;

    public static string RoutingKey(string queueName) => Queue(queueName);
}
