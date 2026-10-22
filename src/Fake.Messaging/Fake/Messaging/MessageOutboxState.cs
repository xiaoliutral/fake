namespace Fake.Messaging;

public enum MessageOutboxState
{
    NotPublished = 0,
    InProgress = 1,
    Published = 2,
    PublishFailed = 3
}
