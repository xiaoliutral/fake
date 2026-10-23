namespace Fake.Messaging;

public static class MessageHeaders
{
    public const string MessageId = "fake-message-id";
    public const string EventId = "fake-event-id";
    public const string EventType = "fake-event-type";
    public const string Kind = "fake-message-kind";

    /// <summary>Rabbit 交换机类型：direct / topic / fanout / headers。</summary>
    public const string ExchangeType = "fake-exchange-type";

    /// <summary>是否 Publisher Confirm（"true"）。</summary>
    public const string PublisherConfirm = "fake-publisher-confirm";
}
