# Fake.Messaging

统一 Outbox / Inbox。EventBus 与独立 Rabbit 队列共用 `fk_msg_outbox` / `fk_msg_inbox`。

## 包边界

| 包 | 职责 | 建议引用层 |
|----|------|-------------|
| `Fake.Messaging` | `Message`、发布路由、`IMessageHandle<>` 发现、Outbox/Inbox 抽象；默认 `DirectMessagePublisher` | Domain/Application |
| `Fake.Messaging.RabbitMQ` | Rabbit 传输 + 消费行为（`RabbitMQ:Messaging`）；**不依赖 EF** | Application |
| `Fake.Messaging.EntityFrameworkCore` | Outbox/Inbox 存储、`ReliableMessagePublisher`、`AddMessagingDbContext` | Api / Infrastructure |

宿主组合：Application → `FakeMessagingRabbitMqModule`；Api → `FakeMessagingEntityFrameworkCoreModule` + `AddMessagingDbContext`（以及可选 Dashboard）。

配置两层，不交叉：

| 层 | 配置 | 归属包 | 内容 |
|----|------|--------|------|
| 路由（发布=订阅） | `Messaging:DefaultTopic` / `DefaultTopicType` + `[MessageRoute]` | **Fake.Messaging** | Name / Topic / TopicType（与传输无关） |
| 消费 / 发布行为 | `RabbitMQ:Messaging`（`IOptionsMonitor` 可热更新） | Fake.Messaging.RabbitMQ | `RetryCount`（发布 Polly）/ `HandlerRetryCount`（消费）/ Prefetch / DLX / UoW；`Handlers` 按名覆盖并发 |

## Dashboard（多微服务）

每个服务进程挂自己的 Dashboard（本机 Outbox/Inbox/本进程订阅）。分布式下**不要**指望一个页面看全集群：

1. **推荐（现状）**：每服务一个入口，如 `TrackCenter /eventbus`、`OrderApi /messaging`；用 `Messaging:Dashboard:ServiceName` 区分标题。
2. **运维聚合（可选后续）**：网关/Consul 列出各服务 Dashboard URL，或独立 Admin 调各服务 `/api/*` 聚合；共享 Outbox 表时再加 `ServiceName` 列过滤。
3. **Subscriptions** 永远是「当前进程」的 Handler；跨服务拓扑看 Rabbit 管理台或各服务各自 Dashboard。

`[MessageSubscribe]` 特例：`Name` / `Group` / `PrefetchCount` / `ExternalQueue` / `Provider` / `ConnectionName`。

并发优先级：`Attribute.PrefetchCount` → `RabbitMQ:Messaging:Handlers:{Handler短名|全名|Queue}` → 全局 `PrefetchCount`（**`0`= `Environment.ProcessorCount`**，对齐 CAP 并行订阅默认线程数）。配置热更新会重打 `BasicQos`。

> 说明：CAP 的 `ConsumerThreadCount` 默认仍是 **1**；核心数默认出现在 `SubscriberParallelExecuteThreadCount`（需显式开并行）。Messaging 独立队列无跨 Handler 顺序约束，故全局 Prefetch 直接默认核心数。

## 发布

```csharp
[MessageRoute("TrackCenter.TrackUpsert")]
public class TrackUpsertEvent : Message { }

await messagePublisher.PublishAsync(evt);
```

## 消费

```csharp
public class TrackDeleteEventHandler
    : IMessageHandle<TrackDeleteEvent>, ITransientDependency
{
    public Task HandleAsync(TrackDeleteEvent e, CancellationToken ct) => ...;
}

[MessageSubscribe("TrackRefresh.Q", PrefetchCount = 1, ExternalQueue = true)]
public class TrackRefreshEventHandler
    : IMessageHandle<InterfaceRelayForwardReplayEvent>, ITransientDependency
{
    public Task HandleAsync(InterfaceRelayForwardReplayEvent e, CancellationToken ct) => ...;
}
```

流程：`IMessageHandle<T>` 收集 → Finder + **同一** `IMessageRouteResolver` 解析路由 → Binder 填消费 Options → 反序列化 → `HandleAsync`。
