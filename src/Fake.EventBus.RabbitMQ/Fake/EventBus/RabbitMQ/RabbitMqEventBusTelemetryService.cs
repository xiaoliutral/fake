using OpenTelemetry;
using OpenTelemetry.Context.Propagation;

namespace Fake.EventBus.RabbitMQ;

public class RabbitMqEventBusTelemetryService(IOptions<RabbitMqEventBusOptions> options)
{
    private readonly ActivitySource _activitySource = new(options.Value.ActivitySourceName);
    private readonly TextMapPropagator _propagator = Propagators.DefaultTextMapPropagator;

    public Activity? StartActivity(string activityName, ActivityKind kind, ActivityContext parentContext = default)
    {
        return _activitySource.StartActivity(activityName, kind, parentContext);
    }

    public void InjectTraceContext(IBasicProperties properties, ActivityContext contextToInject)
    {
        _propagator.Inject(new PropagationContext(contextToInject, Baggage.Current), properties,
            static (props, key, value) =>
            {
                props.Headers ??= new Dictionary<string, object>();
                props.Headers[key] = value;
            });
    }

    public PropagationContext ExtractTraceContext(IBasicProperties properties)
    {
        return _propagator.Extract(default, properties, static (props, key) =>
        {
            if (props.Headers.TryGetValue(key, out var value))
            {
                return new[] { Encoding.UTF8.GetString((byte[])value) };
            }

            return Array.Empty<string>();
        });
    }

    private static void SetActivityContext(Activity? activity, string routingKey, string operation)
    {
        if (activity is null) return;

        // These tags are added demonstrating the semantic conventions of the OpenTelemetry messaging specification
        // https://github.com/open-telemetry/semantic-conventions/blob/main/docs/messaging/messaging-spans.md
        activity.SetTag("messaging.operation.name", operation);
        activity.SetTag("messaging.system", "rabbitmq");
        activity.SetTag("messaging.destination_kind", "queue");
        activity.SetTag("messaging.destination.name", routingKey);
        activity.SetTag("messaging.rabbitmq.routing_key", routingKey);
    }
}