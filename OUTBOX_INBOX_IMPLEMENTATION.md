# Outbox + Inbox 模式实现总结

## ✅ 已完成的工作

### 1. Inbox 模式实现（消费端幂等性）

**新增文件：**
- `Fake.EventBus/Fake/EventBus/IInboxEventLogService.cs` - Inbox 服务接口（放在核心事件总线模块）
- `Fake.EntityFrameworkCore.IntegrationEventLog/Fake/EntityFrameworkCore/IntegrationEventLog/InboxEventLogEntry.cs` - Inbox 实体
- `Fake.EntityFrameworkCore.IntegrationEventLog/Fake/EntityFrameworkCore/IntegrationEventLog/InboxEventLogService.cs` - Inbox 服务实现

**修改文件：**
- `IntegrationEventLogContext.cs` - 添加了 `InboxEventLog` 表配置
- `RabbitMqEventBus.cs` - 在 `ProcessingEventAsync` 方法中集成了 Inbox 幂等性检查

### 2. Outbox 后台发布服务

**新增文件：**
- `OutboxPublisherBackgroundService.cs` - 定期扫描 Outbox 表并发布未发送的事件

### 3. 模块配置

**修改文件：**
- `FakeEntityFrameworkCoreIntegrationEventLogModule.cs` - 注册了 Inbox 服务和后台发布服务

### 4. 文档

**新增文件：**
- `README.md` - 完整的使用指南和架构说明

## 🔍 工作原理

### Outbox 模式（发送端）

```
业务代码:
  using (var tx = db.BeginTransaction()) {
    db.Orders.Add(order);                           // 1. 写业务数据
    eventLogService.SaveEventAsync(event, tx);      // 2. 写事件到 Outbox（同事务）
    tx.Commit();                                    // 3. 原子提交
  }

后台服务:
  每 10 秒扫描 Outbox 表 → 发送到 RabbitMQ → 标记为已发布
```

### Inbox 模式（接收端）

```
RabbitMQ 消息到达 → 检查 Inbox 表:
  - 已存在？直接 Ack，跳过处理
  - 不存在？执行业务逻辑 → 写入 Inbox 表 → Ack
```

## 📋 后续步骤

### 1. 清理旧文件（必须）

**删除以下文件**（已被新接口替代）：
```
Fake.EntityFrameworkCore.IntegrationEventLog/Fake/EntityFrameworkCore/IntegrationEventLog/IInboxEventLogService.cs
```

这个文件在之前的实现中被创建，但接口已经移到了 `Fake.EventBus` 项目中。

### 2. 数据库迁移（必须）

```bash
# 进入 IntegrationEventLog 项目目录
cd src/Fake.EntityFrameworkCore.IntegrationEventLog

# 添加迁移
dotnet ef migrations add AddInboxEventLogTable --context IntegrationEventLogContext

# 更新数据库
dotnet ef database update --context IntegrationEventLogContext
```

### 3. 验证集成

**检查清单：**
- [ ] 项目编译成功
- [ ] 数据库迁移成功
- [ ] 启动应用，确认 `OutboxPublisherBackgroundService` 正常启动
- [ ] 发送一个事件，检查 `IntegrationEventLog` 表是否有记录
- [ ] 检查事件是否被后台服务成功发布
- [ ] 消费一个事件，检查 `InboxEventLog` 表是否有记录
- [ ] 重复发送同一个事件，确认不会被重复处理

### 4. 性能优化

**Inbox 表自动清理：** ✅ 已实现

`InboxCleanupBackgroundService` 会自动清理 7 天前的记录，每天执行一次。

## 🎯 关键设计决策

### 1. 接口位置
将 `IInboxEventLogService` 接口放在 `Fake.EventBus` 项目中，而不是 `Fake.EntityFrameworkCore.IntegrationEventLog` 项目中，这样：
- ✅ `RabbitMqEventBus` 可以依赖接口而不依赖 EF Core 实现
- ✅ 保持松耦合，符合依赖倒置原则
- ✅ 未来可以轻松替换为其他存储实现（如 Redis）

### 2. 可选性设计
`RabbitMqEventBus` 中使用 `GetService<IInboxEventLogService>()` 而不是 `GetRequiredService`：
- ✅ 如果不注册服务，代码仍然可以正常运行（降级为"不保证幂等性"模式）
- ✅ 给用户选择的自由

### 3. 后台服务
`OutboxPublisherBackgroundService` 默认每 10 秒扫描一次：
- ✅ 平衡了实时性和数据库压力
- ✅ 可通过配置调整频率

## 🐛 已知限制

1. **Outbox 表与业务表必须在同一个数据库**  
   这是使用数据库事务的前提。如果需要跨数据库，需要引入分布式事务（如 2PC）或 Saga 模式。

2. ~~**Inbox 表会持续增长**~~ ✅ **已解决**  
   `InboxCleanupBackgroundService` 会自动清理 7 天前的记录。

3. ~~**后台发布服务的单点问题**~~ ✅ **已解决**  
   使用数据库行级锁实现了分布式锁，支持多实例部署。每个实例通过原子更新操作抢占事件，避免重复处理。

## 📚 参考资料

- [Transactional Outbox Pattern](https://microservices.io/patterns/data/transactional-outbox.html)
- [Inbox Pattern for Idempotent Message Processing](https://microservices.io/patterns/communication-style/idempotent-consumer.html)
- [RabbitMQ At-least-once Delivery](https://www.rabbitmq.com/reliability.html)

## 💡 对比表：修改前 vs 修改后

| 特性 | 修改前（最大努力投递） | 修改后（Outbox + Inbox） |
|------|----------------------|------------------------|
| 发送端一致性 | ❌ 可能丢消息或发脏消息 | ✅ 强最终一致性 |
| 接收端幂等性 | ❌ 可能重复处理 | ✅ 自动去重 |
| 适用场景 | 非关键业务（日志、通知） | 核心业务（订单、支付） |
| 复杂度 | 低 | 中 |
| 性能开销 | 无 | 轻微（多一次数据库写入） |

---

**实现完成时间**：2025-11-27  
**实现者**：Cascade AI  
**版本**：v1.0
