# Serilog 飞书集成

使用 Serilog 的结构化日志功能，将日志发送到飞书。

## 快速开始

### 1. 基础配置

```csharp
using Serilog;
using Fake.Logging.Serilog;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .WriteTo.FeiShu(options =>
    {
        options.Webhook = "https://open.feishu.cn/open-apis/bot/v2/hook/xxx";
        options.TitlePrefix = "生产环境告警";
        options.BatchSize = 10;
        options.BatchIntervalSeconds = 5;
    }, restrictedToMinimumLevel: LogEventLevel.Warning)
    .CreateLogger();

// 使用
Log.Information("这条不会发送到飞书");
Log.Warning("这条会发送到飞书");
Log.Error("错误信息也会发送");
```

### 2. ASP.NET Core 集成

**Program.cs**
```csharp
using Serilog;
using Fake.Logging.Serilog;

var builder = WebApplication.CreateBuilder(args);

// 配置 Serilog
builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.FeiShu(options =>
    {
        options.Webhook = context.Configuration["FeiShu:Webhook"]!;
        options.TitlePrefix = context.Configuration["FeiShu:TitlePrefix"]!;
    }, restrictedToMinimumLevel: LogEventLevel.Warning));

var app = builder.Build();

// 使用
app.MapGet("/", (ILogger<Program> logger) =>
{
    logger.LogInformation("访问首页");  // 不发送
    logger.LogWarning("警告信息");      // 发送到飞书
    return "Hello World!";
});

app.Run();
```

### 3. 从配置文件读取

**appsettings.json**
```json
{
  "Serilog": {
    "Using": ["Serilog.Sinks.Console", "Fake.Logging"],
    "MinimumLevel": "Information",
    "WriteTo": [
      { "Name": "Console" },
      {
        "Name": "FeiShu",
        "Args": {
          "options": {
            "Webhook": "https://open.feishu.cn/open-apis/bot/v2/hook/xxx",
            "TitlePrefix": "生产环境",
            "BatchSize": 10,
            "BatchIntervalSeconds": 5
          },
          "restrictedToMinimumLevel": "Warning"
        }
      }
    ]
  }
}
```

**Program.cs**
```csharp
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// 从配置文件读取
builder.Host.UseSerilog((context, configuration) =>
    configuration.ReadFrom.Configuration(context.Configuration));

var app = builder.Build();
app.Run();
```

## 结构化日志

Serilog 的优势在于结构化日志，飞书 Sink 会自动包含这些信息：

```csharp
Log.Warning("订单处理失败 {OrderId} {UserId} {Amount}", 
    orderId, userId, amount);

// 发送到飞书的消息：
// 订单处理失败 12345 user-001 99.99
// 属性: OrderId=12345, UserId=user-001, Amount=99.99
```

## 异常处理

```csharp
try
{
    // 业务逻辑
}
catch (Exception ex)
{
    Log.Error(ex, "处理订单失败 {OrderId}", orderId);
    // 飞书会收到完整的异常堆栈
}
```

## 与 Microsoft.Extensions.Logging 结合

可以同时使用两种方式：

```csharp
var builder = WebApplication.CreateBuilder(args);

// 1. 使用 Serilog 作为主日志框架
builder.Host.UseSerilog((context, configuration) => configuration
    .WriteTo.Console()
    .WriteTo.FeiShu(options => { ... }));

// 2. 同时添加 Microsoft.Extensions.Logging 的 FeiShu Provider
builder.Logging.AddFeiShu();

var app = builder.Build();
```

**推荐**：选择其中一种即可，避免重复发送。

## 配置对比

| 特性 | Microsoft.Extensions.Logging | Serilog |
|------|----------------------------|---------|
| 结构化日志 | ❌ 较弱 | ✅ 强大 |
| 性能 | ✅ 好 | ✅ 更好 |
| 生态 | ✅ .NET 官方 | ✅ 社区丰富 |
| 配置灵活性 | ⭐⭐⭐ | ⭐⭐⭐⭐⭐ |
| 学习曲线 | ⭐⭐ | ⭐⭐⭐ |

## 推荐方案

### 新项目
使用 **Serilog**，功能更强大，生态更丰富。

### 现有项目
- 已使用 `ILogger`：添加 `FeiShuLoggerProvider`
- 已使用 Serilog：添加 `FeiShuSink`

## 完整示例

```csharp
using Serilog;
using Serilog.Events;
using Fake.Logging.Serilog;

var builder = WebApplication.CreateBuilder(args);

// 配置 Serilog
builder.Host.UseSerilog((context, services, configuration) => configuration
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("System", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "MyApp")
    .Enrich.WithProperty("Environment", context.HostingEnvironment.EnvironmentName)
    .WriteTo.Console(
        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.File(
        path: "logs/app-.log",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30)
    .WriteTo.FeiShu(options =>
    {
        options.Webhook = context.Configuration["FeiShu:Webhook"]!;
        options.TitlePrefix = $"{context.HostingEnvironment.EnvironmentName}告警";
        options.BatchSize = 20;
        options.BatchIntervalSeconds = 10;
    }, restrictedToMinimumLevel: LogEventLevel.Warning));

var app = builder.Build();

app.MapGet("/test", (ILogger<Program> logger) =>
{
    logger.LogInformation("普通日志");
    logger.LogWarning("警告 {Time}", DateTime.Now);
    return "OK";
});

try
{
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "应用启动失败");
}
finally
{
    Log.CloseAndFlush();
}
```

## 注意事项

1. **性能**：Serilog 性能优于 Microsoft.Extensions.Logging
2. **批量发送**：两种方式都支持批量发送，减少 HTTP 请求
3. **Debug 模式**：开发环境不会发送到飞书
4. **异常处理**：Serilog 的异常处理更优雅
5. **结构化属性**：Serilog 的结构化日志会自动包含在飞书消息中
