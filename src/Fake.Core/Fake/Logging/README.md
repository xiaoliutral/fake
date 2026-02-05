# 飞书日志使用指南

## 架构设计

使用标准的 .NET `ILogger` 接口，通过自定义 `LoggerProvider` 自动将日志发送到飞书，**业务代码无需改动**。

> 💡 **Serilog 用户**：如果你使用 Serilog，请查看 [Serilog 集成文档](./Serilog/README.md)

## 快速开始

### 方式 1：使用配置文件（推荐）

**appsettings.json**
```json
{
  "Logging": {
    "FeiShu": {
      "IsEnabled": true,
      "MinimumLevel": "Information",
      "FeiShuMinimumLevel": "Warning",
      "NotificationOptions": {
        "Webhook": "https://open.feishu.cn/open-apis/bot/v2/hook/your-webhook-key",
        "TitlePrefix": "生产环境告警",
        "Timeout": 20,
        "QueueCapacity": 500,
        "BatchSize": 10,
        "BatchIntervalSeconds": 5
      }
    }
  }
}
```

**Program.cs**
```csharp
using Fake.Logging;

var builder = WebApplication.CreateBuilder(args);

// 只需一行代码，自动从配置文件读取
builder.Logging.AddFeiShu();

var app = builder.Build();
```

### 方式 2：代码配置

**Program.cs**
```csharp
using Fake.Logging;
using Microsoft.Extensions.Logging;

var builder = WebApplication.CreateBuilder(args);

// 在代码中配置
builder.Logging.AddFeiShu(config =>
{
    config.IsEnabled = true;
    config.MinimumLevel = LogLevel.Information;
    config.FeiShuMinimumLevel = LogLevel.Warning;
    
    config.NotificationOptions = new FeiShuNoticeOptions
    {
        Webhook = "https://open.feishu.cn/open-apis/bot/v2/hook/xxx",
        TitlePrefix = "生产环境告警",
        BatchSize = 10,
        BatchIntervalSeconds = 5
    };
});

var app = builder.Build();
```

### 2. 使用（业务代码）

```csharp
public class OrderService
{
    private readonly ILogger<OrderService> _logger;

    public OrderService(ILogger<OrderService> logger)
    {
        _logger = logger;
    }

    public async Task CreateOrder(Order order)
    {
        // 普通日志（不会发送到飞书）
        _logger.LogInformation("开始创建订单: {OrderId}", order.Id);

        try
        {
            // 业务逻辑
            await SaveOrderAsync(order);
            
            _logger.LogInformation("订单创建成功: {OrderId}", order.Id);
        }
        catch (Exception ex)
        {
            // Error 级别会自动发送到飞书（因为配置了 FeiShuMinimumLevel = Warning）
            _logger.LogError(ex, "订单创建失败: {OrderId}", order.Id);
            throw;
        }
    }
}
```

## 配置说明

### FeiShuLoggerConfiguration

| 属性 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `IsEnabled` | bool | true | 是否启用飞书日志 |
| `MinimumLevel` | LogLevel | Information | ILogger 最低级别（用于 IsEnabled 判断） |
| `FeiShuMinimumLevel` | LogLevel | Warning | 发送到飞书的最低级别 |
| `NotificationOptions` | FeiShuNoticeOptions | - | 飞书通知配置 |

### FeiShuNoticeOptions

| 属性 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `Webhook` | string | - | 飞书机器人 Webhook 地址 |
| `TitlePrefix` | string | - | 消息标题前缀 |
| `BatchSize` | int | 10 | 批量发送的消息数量 |
| `BatchIntervalSeconds` | int | 5 | 批量发送的时间间隔（秒） |
| `QueueCapacity` | int | 500 | 队列容量 |
| `Timeout` | int | 20 | HTTP 超时时间（秒） |

## 级别映射

| LogLevel | 是否发送到飞书 | 飞书显示 |
|----------|--------------|---------|
| Critical | ✅ | 💀 Critical |
| Error | ✅ | 🔴 Error |
| Warning | ✅ | 🟡 Warn |
| Information | ❌ (可配置) | 🔵 Info |
| Debug | ❌ | - |
| Trace | ❌ | - |

## 高级用法

### 1. 从配置文件读取

**appsettings.json**
```json
{
  "Logging": {
    "FeiShu": {
      "IsEnabled": true,
      "MinimumLevel": "Information",
      "FeiShuMinimumLevel": "Warning",
      "NotificationOptions": {
        "Webhook": "https://open.feishu.cn/...",
        "TitlePrefix": "生产环境",
        "BatchSize": 20,
        "BatchIntervalSeconds": 10
      }
    }
  }
}
```

**Program.cs**
```csharp
builder.Logging.AddFeiShu(); // 自动从配置读取
```

### 2. 环境区分

```csharp
builder.Logging.AddFeiShu(config =>
{
    // 只在生产环境发送
    config.IsEnabled = builder.Environment.IsProduction();
    
    config.FeiShuMinimumLevel = builder.Environment.IsProduction() 
        ? LogLevel.Warning   // 生产环境只发送 Warning+
        : LogLevel.Error;    // 测试环境只发送 Error+
    
    config.NotificationOptions = new FeiShuNoticeOptions
    {
        Webhook = builder.Configuration["FeiShu:Webhook"]!,
        TitlePrefix = builder.Environment.EnvironmentName
    };
});
```

### 3. 结合其他日志提供程序

```csharp
builder.Logging
    .AddConsole()           // 控制台日志
    .AddFile("logs/app.log") // 文件日志
    .AddFeiShu(config =>    // 飞书告警
    {
        config.FeiShuMinimumLevel = LogLevel.Error;
    });
```

### 4. 配置热重载（Hot Reload）

使用配置文件时，支持运行时修改配置，**无需重启应用**：

```json
// 运行时修改 appsettings.json
{
  "Logging": {
    "FeiShu": {
      "FeiShuMinimumLevel": "Information"  // 从 Warning 改成 Information
    }
  }
}
```

**效果**：
- ✅ 配置立即生效，无需重启
- ✅ 新的日志级别立即应用
- ✅ Webhook、批量大小等配置也会更新

**注意**：代码配置（`AddFeiShu(config => {...})`）不支持热重载。

## 优势对比

### ❌ 旧方案（LogHelper）

```csharp
// 业务代码耦合了飞书逻辑
LogHelper.Error("数据库连接失败", isSend: true);

// 未来要发钉钉？改所有代码
DingTalkHelper.Error("数据库连接失败", isSend: true);
```

### ✅ 新方案（ILogger Provider）

```csharp
// 业务代码只依赖标准接口
_logger.LogError("数据库连接失败");

// 未来要发钉钉？只需添加 Provider，业务代码不变
builder.Logging
    .AddFeiShu(...)
    .AddDingTalk(...)
    .AddSlack(...);
```

## 注意事项

1. **Debug 模式不发送**：代码中有 `#if DEBUG` 判断，开发环境不会发送
2. **批量发送**：默认累积 10 条或 5 秒后发送，减少 HTTP 请求
3. **自动重试**：网络失败会重试 3 次（1s, 2s, 5s 递增延迟）
4. **优雅关闭**：应用关闭时会自动发送剩余消息
5. **线程安全**：内部使用 Channel，完全线程安全

## 迁移指南

如果你已经在使用 `LogHelper`，可以这样迁移：

### 步骤 1：配置新的 Provider
```csharp
builder.Logging.AddFeiShu(config => { ... });
```

### 步骤 2：替换业务代码
```csharp
// 旧代码
LogHelper.Error("错误信息", isSend: true);

// 新代码
_logger.LogError("错误信息");
```

### 步骤 3：删除 LogHelper 调用
全局搜索 `LogHelper.` 并替换为 `_logger.Log...`
