# Fake.Messaging

统一 Outbox / Inbox 抽象。EventBus 与独立 Rabbit 队列共用 `fk_msg_outbox` / `fk_msg_inbox`。

## 包

| 包 | 模块 | 职责 |
|----|------|------|
| `Fake.Messaging` | `FakeMessagingModule` | `IMessageOutbox` / `IMessageInbox` / `IMessagePublisher`、Admin 抽象、Envelope、Destination |
| `Fake.Messaging.EntityFrameworkCore` | `FakeMessagingEntityFrameworkCoreModule` | 存储、AdminStore、Dispatcher、清理；宿主需 `AddDbContext<MessagingDbContext>` |
| `Fake.Messaging.RabbitMQ` | `FakeMessagingRabbitMqModule` | 独立队列 Publish/Subscribe |
| `Fake.Messaging.Dashboard` | `FakeMessagingDashboardModule` | Outbox/Inbox 运维 UI；默认路径 `/messaging` |
| `Fake.EventBus.Messaging` | `FakeEventBusMessagingModule` | EventBus ↔ Messaging Outbox 适配（原 IntegrationEventLog） |

## 配置

各包在项目根目录维护 `appsettings.example.json`（仅含本包 Options）：

| 包 | 配置节 | 示例文件 |
|----|--------|----------|
| `Fake.Messaging` | `Messaging` | `src/Fake.Messaging/appsettings.example.json` |
| `Fake.Messaging.Dashboard` | `Messaging:Dashboard` | `src/Fake.Messaging.Dashboard/appsettings.example.json` |
| `Fake.EventBus` | `EventBus:Routing` | `src/Fake.EventBus/appsettings.example.json` |
| `Fake.EventBus.RabbitMQ` | `RabbitMQ:EventBus` | `src/Fake.EventBus.RabbitMQ/appsettings.example.json` |

统一 Outbox/Inbox 扫描 / 锁 / 清理走 `Messaging`。

## Dashboard

```csharp
[DependsOn(typeof(FakeMessagingDashboardModule))]
// 或
services.AddFakeMessagingDashboard(o => o.PathMatch = "/messaging");
app.UseFakeMessagingDashboard(); // 须在 UsePathBase 之后
```

- 核心面板依赖 Messaging Admin + Outbox Dispatcher；按 `Kind`（EventBus / Rabbit）筛选。
- EventBus 专有能力为可选适配（有则显示）：订阅拓扑、Inbox 本地 Reprocess、Metrics、DLX（由 `Fake.EventBus.Messaging` / `Fake.EventBus.RabbitMQ` 注册）。
- 破坏性迁移：不再提供 `Fake.EventBus.Dashboard` / `AddFakeEventBusDashboard` / `EventBus:Dashboard`。原 `/eventbus` 可自行配置 `PathMatch`。

## EventBus

依赖 `FakeEventBusMessagingModule`（DependsOn Messaging.EF）。Outbox/Inbox 直接写 Messaging，无平行 EventLog 抽象。

```csharp
await distributedEventBus.PublishAsync(integrationEvent);
services.AddDbContext<MessagingDbContext>(...);
```

迁移：`InitialMessaging` 建表；`MigrateEventLogToMessaging` 从 `fk_event_*` 拷贝后删旧表。

## 独立队列

```csharp
[DependsOn(typeof(FakeMessagingRabbitMqModule))]

await messagePublisher.PublishAsync(new RabbitEnvelope(
    Exchange: "ex.orders", RoutingKey: "order.export",
    Body: bytes, MessageId: id));

messageConsumer.Subscribe(new RabbitConsumeOptions
{
    Queue = "order.export.q",
    Exchange = "ex.orders",
    RoutingKey = "order.export",
    FetchCount = 10,
    HandlerTimeout = TimeSpan.FromMinutes(5),
    Handler = async (ctx, ct) => { /* ... */ }
});
```

Destination：`eventbus:{routingKey}` / `rabbit:{exchange}/{routingKey}`。  
Inbox 幂等键：`(MessageId, ConsumerGroup)`。
