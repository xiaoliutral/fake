namespace Fake.EventBus.Distributed;

public enum EventRoutingKeyStrategy
{
    /// <summary>类型短名（默认，兼容现有队列绑定）</summary>
    ShortName = 0,

    /// <summary>类型 FullName</summary>
    FullName = 1
}
