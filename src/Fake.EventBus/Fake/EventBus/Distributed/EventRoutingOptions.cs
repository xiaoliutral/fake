namespace Fake.EventBus.Distributed;

/// <summary>
/// 事件路由配置。默认 ShortName；可通过 CustomRoutes 覆盖（key 支持短名或 FullName）。
/// </summary>
public class EventRoutingOptions
{
    public const string SectionName = "EventBus:Routing";

    public EventRoutingKeyStrategy Strategy { get; set; } = EventRoutingKeyStrategy.ShortName;

    /// <summary>
    /// 自定义路由：事件类型名（ShortName 或 FullName）→ routing key
    /// </summary>
    public Dictionary<string, string> CustomRoutes { get; set; } = new(StringComparer.Ordinal);
}
