namespace Fake.EventBus.Distributed;

/// <summary>
/// 集成事件状态（Outbox 和 Inbox 统一状态）
/// </summary>
public enum EventState
{
    // ========== Outbox 状态（发送端）==========
    
    /// <summary>
    /// 未发布
    /// </summary>
    NotPublished = 0,
    
    /// <summary>
    /// 发布中
    /// </summary>
    InProgress = 1,
    
    /// <summary>
    /// 已发布
    /// </summary>
    Published = 2,
    
    /// <summary>
    /// 发布失败
    /// </summary>
    PublishFailed = 3,
    
    // ========== Inbox 状态（接收端）==========
    
    /// <summary>
    /// 消费中
    /// </summary>
    Consuming = 10,
    
    /// <summary>
    /// 消费成功
    /// </summary>
    ConsumeSucceeded = 11,
    
    /// <summary>
    /// 消费失败
    /// </summary>
    ConsumeFailed = 12
}