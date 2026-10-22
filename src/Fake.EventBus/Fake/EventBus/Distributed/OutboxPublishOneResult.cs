namespace Fake.EventBus.Distributed;

public sealed record OutboxPublishOneResult(bool Published, string? Error = null);
