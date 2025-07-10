using Microsoft.Extensions.DependencyInjection;

namespace Fake.EventBus.Local;

public abstract class EventHandlerWrapper
{
    public abstract Task HandleAsync(Event @event, IServiceProvider serviceProvider,
        Func<IEnumerable<EventHandlerExecutor>, Event, CancellationToken, Task> publish,
        CancellationToken cancellationToken);
}

public class EventHandlerWrapperImpl<TEvent> : EventHandlerWrapper where TEvent : Event
{
    public override Task HandleAsync(Event @event, IServiceProvider serviceProvider,
        Func<IEnumerable<EventHandlerExecutor>, Event, CancellationToken, Task> publish,
        CancellationToken cancellationToken)
    {
        var handlers = serviceProvider
            .GetServices<IEventHandler<TEvent>>()
            .Select(handler => new EventHandlerExecutor(handler,
                (theEvent, theToken) => handler.HandleAsync((TEvent)theEvent, theToken)));

        return publish(handlers, @event, cancellationToken);
    }
}