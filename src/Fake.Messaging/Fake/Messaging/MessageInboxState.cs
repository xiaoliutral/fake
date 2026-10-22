namespace Fake.Messaging;

public enum MessageInboxState
{
    Consuming = 10,
    ConsumeSucceeded = 11,
    ConsumeFailed = 12,
    NoHandler = 13
}
