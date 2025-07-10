namespace Fake.EventBus.Local;

public record EventHandlerExecutor(object HandlerInstance, Func<Event, CancellationToken, Task> HandleFunc)
{
    public object HandlerInstance { get; } = HandlerInstance;
    public Func<Event, CancellationToken, Task> HandleFunc { get; } = HandleFunc;
}