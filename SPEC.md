# Fake 框架 AI 开发规范

## 1. 文档定位

本文件是面向 AI 和工程师的新项目开发规范，不是框架原理长文，也不是现有 `docs/Fake框架完整文档.md` 的复制品。

目标只有一个：当后续 AI 基于本仓库编写新项目时，必须以本规范作为默认约束，优先遵循 Fake 的真实源码能力、模块边界和接入方式，避免沿用过时文档名、臆造包能力或绕开框架约定。

适用对象：

- 基于 Fake 新建 Console / Web API / 测试项目
- 基于 Fake 增加新的业务模块、应用服务、仓储、事件处理器
- 为现有业务模块补齐授权、审计、分布式事件、租户、对象映射等能力

不适用对象：

- 修改 Fake 框架内核实现本身
- 为不存在的 Fake 能力“脑补设计”

## 2. 事实来源与兼容纠偏

### 2.1 事实来源优先级

后续生成代码时，按以下优先级判断真实行为：

1. `src/`：唯一权威来源。模块类、扩展方法、基类、接口、Options、默认实现都以源码为准。
2. `simple/`、`app/`、`modules/`：真实接入示例，使用前仍需回查源码。
3. `docs/` 与 `README.md`：仅作语义补充，不可覆盖源码事实。

### 2.2 已确认的兼容纠偏

以下名称和用法必须以源码为准：

| 历史/文档写法 | 当前源码写法 | 说明 |
| --- | --- | --- |
| `AddFakeApplication<TStartupModule>()` | `AddApplication<TStartupModule>()` | 当前公开入口在 `src/Fake.Core/Microsoft/Extensions/DependencyInjection/FakeApplicationServiceCollectionExtensions.cs.cs` |
| `FakeAutoMapperModule` | `FakeObjectMappingAutoMapperModule` | 新项目依赖模块时必须使用真实模块类名 |
| `FakeMapsterModule` 或推测性命名 | `FakeObjectMappingMasterModule` | 类名看起来像命名偏差，但必须以源码为准 |
| 直接依赖 `FakeCoreModule` | 不需要显式依赖 | `FakeCoreModule` 由模块加载器自动加入 |

### 2.3 当前未完整提供的包能力

以下包不要作为默认选型：

- `Fake.EventBus.RabbitMQ.OpenTelemetry`：当前目录仅有 `FodyWeavers.xml` 与 `FodyWeavers.xsd`，没有可用实现。
- `Fake.Caching.StackExchangeRedis`：当前模块类存在，但未注册具体缓存服务。
- `Fake.Caching`：当前是逻辑基包/占位模块，不提供统一 `ICache` 抽象。

## 3. Fake 项目标准骨架

### 3.1 标准分层

新项目默认采用以下四层或三层结构，不应把所有配置都堆进 `Program.cs`：

| 层 | 推荐项目 | 主要职责 |
| --- | --- | --- |
| Host / HttpApi | `YourApp.HttpApi` 或 `YourApp.Api` | 启动程序、Web 管道、动态 API 暴露、Swagger、认证/授权接入 |
| Application | `YourApp.Application` | 应用服务、DTO、对象映射配置、事务边界内的业务编排 |
| Domain | `YourApp.Domain` | 聚合根、实体、值对象、领域服务、领域事件、仓储接口、业务异常 |
| Infrastructure | `YourApp.EntityFrameworkCore` 或 `YourApp.SqlSugarCore` | DbContext、仓储实现、外部基础设施接入 |

### 3.2 推荐模块命名

每个项目建议有且仅有一个 `FakeModule` 派生类，并按项目职责命名：

- `YourAppDomainModule`
- `YourAppApplicationModule`
- `YourAppEntityFrameworkCoreModule` 或 `YourAppSqlSugarCoreModule`
- `YourAppHttpApiModule`

不要为了省事只写一个“大模块”把所有配置塞进去。Fake 的设计前提就是按模块分摊配置。

### 3.3 推荐依赖组织

典型 Web API 项目建议按以下方式组织模块依赖：

```csharp
[DependsOn(typeof(FakeDomainDrivenDesignModule))]
public class YourAppDomainModule : FakeModule
{
}

[DependsOn(
    typeof(YourAppDomainModule),
    typeof(FakeObjectMappingAutoMapperModule) // 或 FakeObjectMappingMasterModule，二选一
)]
public class YourAppApplicationModule : FakeModule
{
}

[DependsOn(
    typeof(YourAppDomainModule),
    typeof(FakeEntityFrameworkCoreModule) // 或 FakeSqlSugarCoreModule，二选一
)]
public class YourAppEntityFrameworkCoreModule : FakeModule
{
}

[DependsOn(
    typeof(FakeAutofacModule),
    typeof(FakeAspNetCoreModule),
    typeof(YourAppApplicationModule),
    typeof(YourAppEntityFrameworkCoreModule)
)]
public class YourAppHttpApiModule : FakeModule
{
}
```

选择原则：

- ORM 只选一个：`Fake.EntityFrameworkCore` 或 `Fake.SqlSugarCore`
- 对象映射只选一个：`Fake.ObjectMapping.AutoMapper` 或 `Fake.ObjectMapping.Mapster`
- 需要 AOP 时默认使用 Autofac：授权、审计、工作单元都依赖代理链

## 4. 核心生命周期与 DI 规则

### 4.1 `FakeApplication` 生命周期

核心对象是 `FakeApplication`，其职责是：

- 记录启动模块类型
- 加载模块链
- 运行 `ConfigureServices`
- 在 `InitializeApplication()` 时构建 `ServiceProvider`
- 触发模块 `ConfigureApplication`

AI 生成项目时必须理解以下顺序：

1. 创建 `FakeApplication`
2. 加载模块并按拓扑顺序排序
3. 依次执行模块的 `PreConfigureServices`、`ConfigureServices`、`PostConfigureServices`
4. 调用 `InitializeApplication()`
5. 依次执行模块的 `PreConfigureApplication`、`ConfigureApplication`、`PostConfigureApplication`
6. 关闭时执行 `Shutdown`

### 4.2 模块 Hook

所有模块都可以重写以下 Hook：

- `PreConfigureServices`
- `ConfigureServices`
- `PostConfigureServices`
- `PreConfigureApplication`
- `ConfigureApplication`
- `PostConfigureApplication`
- `Shutdown`

使用原则：

- 服务注册放在 `ConfigureServices`
- 中间件和运行态初始化放在 `ConfigureApplication`
- 拦截器装配、多模块约定等前置动作放在 `PreConfigureServices`

### 4.3 自动服务注册规则

默认自动注册依赖于 Fake 的服务生命周期接口：

- `ITransientDependency`
- `IScopedDependency`
- `ISingletonDependency`

AI 生成代码时应优先使用这三个标记接口，而不是手工重复写大量 `AddTransient/AddScoped/AddSingleton`。

可配套使用的规则：

- `ExposeServicesAttribute`：显式暴露服务类型
- `DependencyAttribute`：控制生命周期、`Replace`、`TryAdd`
- `DisableServiceRegistrationAttribute`：禁止自动注册
- `FakeModule.SkipServiceRegistration`：跳过整个模块程序集的自动注册

### 4.4 Autofac 规则

`FakeAutofacModule` 本身只声明模块依赖，不会自动把宿主切换成 Autofac。要让拦截器真正生效，必须显式启用 Autofac：

- Web/Host：`builder.Host.UseAutofac()`
- Console/Test：`options.UseAutofac()`

这是硬性要求。若缺少这一步，授权、审计、工作单元等代理能力可能无法按预期生效。

## 5. 标准接入模板

### 5.1 Console 最小模板

```csharp
using Fake;

using var application = FakeApplicationFactory.Create<YourConsoleModule>(options =>
{
    options.Configuration.CommandLineArgs = args;
    options.UseAutofac();
});

application.InitializeApplication();

// 使用 application.ServiceProvider 获取服务
```

参考：`simple/SimpleConsoleDemo/Program.cs`

### 5.2 Web 最小模板

```csharp
using Fake.AspNetCore;
using Fake.Autofac;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseAutofac();
builder.Services.AddApplication<YourHttpApiModule>();

var app = builder.Build();
app.InitializeApplication();
app.Run();
```

参考：`simple/SimpleWebDemo/Program.cs`、`app/SimpleAdmin.Api/Program.cs`

### 5.3 Web 模块最小模板

```csharp
using Fake.AspNetCore;
using Fake.AspNetCore.Mvc;
using Fake.Autofac;
using Fake.Modularity;

[DependsOn(typeof(FakeAutofacModule))]
[DependsOn(typeof(FakeAspNetCoreModule))]
public class YourHttpApiModule : FakeModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.Configure<FakeAspNetCoreMvcOptions>(options =>
        {
            options.ApplicationServices2Controller<YourApplicationModule>();
        });

        context.Services.AddFakeSwaggerGen(addSecurity: true);
    }

    public override void ConfigureApplication(ApplicationConfigureContext context)
    {
        var app = context.GetWebApplication();

        app.UseFakeExceptionHandling();
        app.UseRouting();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();
    }
}
```

关键点：

- `ApplicationServices2Controller<TModule>()` 用于把应用服务程序集自动暴露为控制器
- 默认 `RootPath` 为 `"api"`，可在 `settings.RootPath` 中覆盖
- `FakeAspNetCoreModule` 已经把控制器注册成服务，以便代理拦截器生效

### 5.4 测试最小模板

纯应用集成测试优先使用：

```csharp
public class YourTests : ApplicationTest<YourStartupModule>
{
}
```

AspNetCore 集成测试优先使用：

```csharp
public class YourApiTests : AspNetCoreIntegrationTest<YourHttpApiModule>
{
}
```

也可通过 `UseFakeTestServer<TStartupModule>()` 构建测试主机。

参考：`src/Fake.Testing/Fake/Testing/ApplicationTest.cs`、`src/Fake.AspNetCore.Testing/Fake/AspNetCore/Testing/AspNetCoreIntegrationTest.cs`

## 6. AI 生成代码硬性规则

后续 AI 写 Fake 项目时，默认必须遵守以下规则。

### 6.1 必须遵守

1. 使用模块化组织，不把框架配置全部写在 `Program.cs`。
2. 需要 AOP 时显式启用 Autofac。
3. 应用层默认继承 `ApplicationService`，而不是自造一套基类。
4. Web 暴露应用服务时，使用 `FakeAspNetCoreMvcOptions.ApplicationServices2Controller<TModule>()`。
5. 领域持久化默认通过 `IRepository` / `IEfCoreRepository` / `ISqlSugarRepository`。
6. 事务边界默认依赖 `ApplicationService` 的 `IUnitOfWorkEnabled` 能力，或显式使用 `UnitOfWorkAttribute`。
7. 本地化默认通过 `FakeLocalizationOptions` 与虚拟 JSON 资源加载，不要直接散落字符串常量。
8. 对象映射必须显式选择 AutoMapper 或 Mapster 的其中一个实现包。

### 6.2 默认优先

- 优先使用 `Fake.EntityFrameworkCore`，除非项目已明确采用 SqlSugar。
- 优先使用 `Fake.ObjectMapping.AutoMapper`，除非项目已有 Mapster 资产。
- 优先使用 `ApplicationService` + DTO + 仓储，不直接在控制器中写查询和事务细节。
- 优先通过模块 `ConfigureServices/ConfigureApplication` 组织配置，不在 `Program.cs` 中堆积重复代码。
- 优先通过 DI 获取能力，不直接依赖静态辅助器。

### 6.3 明确禁止

- 不要在新代码中使用 `AddFakeApplication<T>()`。
- 不要显式 `[DependsOn(typeof(FakeCoreModule))]`。
- 不要同时默认启用两套 ORM。
- 不要同时默认启用两套对象映射 Provider。
- 不要把 `Fake.Caching` 当成已完成的统一缓存抽象。
- 不要引用 `Fake.EventBus.RabbitMQ.OpenTelemetry` 作为可用能力。
- 不要在未实现 `IPermissionDefiner` 和 `IPermissionStore` 的前提下假设权限系统可用。

## 7. 常用配置节点

AI 生成配置文件时，优先按以下节点组织：

| 配置节点 | 用途 |
| --- | --- |
| `ConnectionStrings` | 默认连接字符串解析 |
| `DataSeed` | 数据种子开关，`FakeCoreModule` 会读取 `DataSeedOptions.Enable` |
| `FeiShuNotice` | 飞书通知服务与 Serilog 飞书 Sink 依赖 |
| `JwtSettings` | `AddFakeJwtAuthentication()` 读取的 JWT 配置 |
| `Redis` | `Fake.Caching.FreeRedis` 读取的 Redis 连接配置 |
| `RabbitMQ` | `FakeRabbitMqOptions` 所在配置节 |
| `IntegrationEventLog:OutboxPublisher` | Outbox 发布器配置 |
| `IntegrationEventLog:InboxCleanup` | Inbox 清理配置 |
| `Consul` | Consul 客户端配置 |
| `Consul:Register` | Consul 服务注册配置 |

## 8. 包总览矩阵

状态说明：

- `默认`：建议作为常规 Fake 项目默认选型
- `可选`：按业务场景启用
- `轻量`：存在，但能力主要在扩展方法或业务实现中
- `占位`：当前不要作为默认能力

| 包 | 分组 | 默认程度 | 状态简述 |
| --- | --- | --- | --- |
| `Fake.Core` | 基础 | 默认 | 框架核心，自动加载 |
| `Fake.Castle` | 基础 | 默认 | 动态代理与拦截器基础 |
| `Fake.Autofac` | 基础 | 默认 | 让 Fake 与 Autofac 协作 |
| `Fake.Security` | 横切 | 默认 | 当前用户、Claims、Principal 访问 |
| `Fake.VirtualFileSystem` | 基础 | 默认 | 虚拟文件与嵌入资源基础 |
| `Fake.Localization` | 横切 | 默认 | 本地化资源系统 |
| `Fake.UnitOfWork` | 横切 | 默认 | 工作单元与事务边界 |
| `Fake.Auditing` | 横切 | 默认 | 审计拦截与审计上下文 |
| `Fake.Authorization` | 横切 | 默认 | 动态策略授权，需补齐权限实现 |
| `Fake.ObjectMapping` | 基础 | 默认 | 映射抽象层，单独使用会抛异常 |
| `Fake.ObjectMapping.AutoMapper` | 基础 | 默认 | AutoMapper Provider |
| `Fake.ObjectMapping.Mapster` | 基础 | 可选 | Mapster Provider |
| `Fake.DomainDrivenDesign` | DDD/应用 | 默认 | 实体、聚合根、值对象、仓储、应用服务基类 |
| `Fake.EntityFrameworkCore` | 数据 | 默认 | EF Core 集成与仓储支持 |
| `Fake.SqlSugarCore` | 数据 | 可选 | SqlSugar 集成与仓储支持 |
| `Fake.MultiTenant` | 数据 | 可选 | 租户解析与连接串切换 |
| `Fake.Caching` | 缓存 | 轻量 | 基包/占位，不提供统一缓存抽象 |
| `Fake.Caching.FreeRedis` | 缓存 | 可选 | FreeRedis 接入，提供 `IRedisClient` |
| `Fake.Caching.StackExchangeRedis` | 缓存 | 占位 | 模块存在，但无具体注册 |
| `Fake.EventBus` | 事件 | 默认 | 本地事件总线 |
| `Fake.RabbitMQ` | 事件 | 可选 | RabbitMQ 连接与通道池 |
| `Fake.EventBus.RabbitMQ` | 事件 | 可选 | RabbitMQ 分布式事件总线 |
| `Fake.EventBus.RabbitMQ.OpenTelemetry` | 事件 | 占位 | 当前无实现 |
| `Fake.EntityFrameworkCore.IntegrationEventLog` | 事件/数据 | 可选 | Outbox/Inbox 持久化支持 |
| `Fake.Consul` | 微服务 | 可选 | Consul 注册、发现、配置热加载 |
| `Fake.Serilog` | 运维 | 可选 | FeiShu Sink 与运行时服务桥接 |
| `Fake.AspNetCore` | Web | 默认 | Web 集成、动态 API、Swagger、中间件辅助 |
| `Fake.AspNetCore.Grpc` | Web/微服务 | 可选 | gRPC 客户端负载均衡与服务端扩展 |
| `Fake.Testing` | 测试 | 默认 | 应用层集成测试基类 |
| `Fake.AspNetCore.Testing` | 测试 | 默认 | Web 集成测试基类 |

## 9. 包说明卡

### 9.1 基础与横切

#### `Fake.Core`

- 用途：Fake 核心基础设施。提供 `FakeApplication`、模块系统、DI 自动注册、JSON、时钟、ID 生成、数据种子、飞书通知等。
- 主要入口：`FakeApplication`、`FakeApplicationFactory`、`FakeModule`、`DependsOnAttribute`、`ITransientDependency` / `IScopedDependency` / `ISingletonDependency`、`ExposeServicesAttribute`、`DependencyAttribute`。
- 典型配置：Console 中用 `FakeApplicationFactory.Create<T>()`；Web/Test 中用 `AddApplication<T>()`。
- 注意：自动加载，不需要手工依赖；`IDataSeedContributor` 由 `DataSeedOptions.Enable` 控制是否执行。
- 参考：`src/Fake.Core/`

#### `Fake.Castle`

- 用途：AOP 基础层。读取 `[FakeIntercept]` 并准备异步拦截器适配。
- 主要入口：`FakeCastleModule`、`FakeInterceptAttribute`、`IFakeInterceptor`、`FakeAsyncDeterminationInterceptor<>`。
- 何时使用：需要自定义拦截器或依赖授权/审计/工作单元等代理能力时。
- 注意：通常通过 `Fake.Autofac` 间接使用，不单独作为业务模块依赖目标。
- 参考：`src/Fake.Castle/`

#### `Fake.Autofac`

- 用途：让 Fake 使用 Autofac 作为真正的 `ServiceProviderFactory`。
- 主要入口：`FakeAutofacModule`、`UseAutofac(this IHostBuilder)`、`UseAutofac(this FakeApplicationCreationOptions)`。
- 何时使用：只要项目依赖拦截器链，就默认启用。
- 注意：仅依赖模块不够，宿主必须显式调用 `UseAutofac()`。
- 参考：`src/Fake.Autofac/`

#### `Fake.Security`

- 用途：提供当前用户与当前 Principal 访问。
- 主要入口：`ICurrentUser`、`ICurrentPrincipalAccessor`、`CurrentUser`、`FakeClaimsPrincipalFactoryOptions`、`IFakeClaimsPrincipalContributor`。
- 何时使用：需要读取当前用户 Id、角色、租户 Id、Claims 时。
- 注意：Web 场景下 `Fake.AspNetCoreModule` 会把 `ICurrentPrincipalAccessor` 替换为 `HttpContextCurrentPrincipalAccessor`。
- 参考：`src/Fake.Security/`

#### `Fake.VirtualFileSystem`

- 用途：统一管理嵌入文件、动态文件和虚拟文件访问。
- 主要入口：`FakeVirtualFileSystemModule`、`IVirtualFileProvider`、`IDynamicFileProvider`、`AddFakeVirtualFileSystem<TModule>(root)`。
- 何时使用：加载嵌入式 JSON 本地化资源、静态文件、虚拟资源时。
- 注意：本地化系统直接依赖它；资源路径要与模块嵌入目录保持一致。
- 参考：`src/Fake.VirtualFileSystem/`

#### `Fake.Localization`

- 用途：提供资源字典、本地化工厂、虚拟 JSON 资源加载。
- 主要入口：`FakeLocalizationModule`、`FakeLocalizationOptions`、`LoadVirtualJson()`、`IFakeStringLocalizerFactory`。
- 何时使用：需要多语言文案、异常文案、领域/应用服务 `L` 本地化对象时。
- 典型配置：在模块中配置 `options.Resources.Add<YourResource>("zh").LoadVirtualJson("/Localization/Resources");`。
- 注意：建议设置 `FakeLocalizationOptions.DefaultResourceType` 或在基类中显式指定 `LocalizationResource`，否则 `L` 可能不可用。
- 参考：`src/Fake.Localization/`

#### `Fake.UnitOfWork`

- 用途：工作单元、事务 API、数据库 API 容器与拦截器。
- 主要入口：`FakeUnitOfWorkModule`、`IUnitOfWork`、`IUnitOfWorkManager`、`UnitOfWorkAttribute`、`DisableUnitOfWorkAttribute`、`FakeUnitOfWorkOptions`。
- 何时使用：几乎所有带持久化的业务服务都会依赖它。
- 注意：`ApplicationService` 已实现 `IUnitOfWorkEnabled`，通常不需要重复标注；若不启用 Autofac，拦截器可能无法生效。
- 参考：`src/Fake.UnitOfWork/`

#### `Fake.Auditing`

- 用途：方法审计、实体变更审计、审计日志上下文。
- 主要入口：`FakeAuditingModule`、`IAuditingManager`、`IAuditingStore`、`AuditedAttribute`、`DisableAuditingAttribute`、`FakeAuditingOptions`。
- 何时使用：需要请求审计、应用服务审计、领域实体审计时。
- 典型配置：Web 项目额外调用 `AddFakeAspNetCoreAuditing()`，把默认 Store 替换为 `AspNetCoreAuditingStore`。
- 注意：默认 Store 为 `SimpleAuditingStore`；`ApplicationService` 默认实现了 `IAuditingEnabled`。
- 参考：`src/Fake.Auditing/`

#### `Fake.Authorization`

- 用途：基于策略的动态授权、权限检查与授权拦截。
- 主要入口：`FakeAuthorizationModule`、`IPermissionDefiner`、`IPermissionStore`、`IPermissionChecker`、`IPermissionManager`、`FakeAuthorizationPolicyProvider`。
- 何时使用：需要 `[Authorize]`、基于角色/用户的动态权限控制时。
- 典型配置：实现 `IPermissionDefiner` 定义权限树，实现 `IPermissionStore` 提供持久化检查逻辑。
- 注意：默认 `NullPermissionStore` 永远返回 `false`，不实现自定义 Store 就不要假设权限系统可用。
- 参考：`src/Fake.Authorization/`、`modules/rbac/`

#### `Fake.ObjectMapping`

- 用途：对象映射抽象层。
- 主要入口：`IObjectMapper`、`IObjectMapper<TSource, TDestination>`、`IObjectMappingProvider`。
- 何时使用：任何 DTO 与实体映射、查询结果投影映射。
- 注意：默认 `NotImplementedObjectMappingProvider` 会直接抛 `NotImplementedException`；必须叠加 AutoMapper 或 Mapster 包。
- 参考：`src/Fake.ObjectMapping/`

#### `Fake.ObjectMapping.AutoMapper`

- 用途：用 AutoMapper 作为 Fake 的映射 Provider。
- 主要入口：`FakeObjectMappingAutoMapperModule`、`FakeAutoMapperOptions`、`ScanProfiles<TModule>()`、`AddProfile<TProfile>()`。
- 何时使用：默认推荐的映射实现。
- 注意：如果项目已经选择 Mapster，就不要同时启用它。
- 参考：`src/Fake.ObjectMapping.AutoMapper/`

#### `Fake.ObjectMapping.Mapster`

- 用途：用 Mapster 作为 Fake 的映射 Provider。
- 主要入口：`FakeObjectMappingMasterModule`、`FakeMapsterOptions`、`Scan<TModule>(compile)`。
- 何时使用：项目已有 Mapster 资产或明确偏好 Mapster 时。
- 注意：真实模块类名是 `FakeObjectMappingMasterModule`，不是推测性命名。
- 参考：`src/Fake.ObjectMapping.Mapster/`

### 9.2 DDD、应用与数据

#### `Fake.DomainDrivenDesign`

- 用途：Fake 的 DDD 主体包，同时也提供应用层基类与应用服务接口。
- 主要入口：`AggregateRoot`、`Entity`、`ValueObject`、`Enumeration`、`DomainService`、`ApplicationService`、`IApplicationService`、`IRepository`、`BusinessException`。
- 何时使用：新业务项目几乎默认依赖它。
- 关键事实：`ApplicationService` 已实现 `ITransientDependency`、`IUnitOfWorkEnabled`、`IAuditingEnabled`，并通过 `LazyServiceProvider` 暴露 `ObjectMapper`、`CurrentUser`、`DataFilter`、`UnitOfWorkManager`、`L`。
- 注意：业务异常优先用 `BusinessException`；仓储接口面向聚合根。
- 参考：`src/Fake.DomainDrivenDesign/`

#### `Fake.EntityFrameworkCore`

- 用途：EF Core 集成、UoW 下的 DbContext 获取、仓储支持、命令拦截与实体变更辅助。
- 主要入口：`FakeEntityFrameworkCoreModule`、`EfCoreDbContext<TDbContext>`、`IEfDbContextProvider<TDbContext>`、`IEfCoreRepository<TDbContext, TEntity>`、`FakeDbCommandInterceptor`。
- 何时使用：项目采用 EF Core 时。
- 注意：仓储实现应优先使用包内基类/接口，不要自己重造 UoW 兼容层。
- 参考：`src/Fake.EntityFrameworkCore/`

#### `Fake.EntityFrameworkCore.IntegrationEventLog`

- 用途：基于 EF Core 的 Outbox/Inbox 持久化支持。
- 主要入口：`FakeEntityFrameworkCoreIntegrationEventLogModule`、`IOutboxEventLogService`、`IInboxEventLogService`、`OutboxPublisherBackgroundService`、`InboxCleanupBackgroundService`、`OutboxPublisherOptions`、`InboxCleanupOptions`。
- 何时使用：需要分布式事件可靠发布、消费幂等、后台补偿时。
- 典型搭配：与 `Fake.EventBus.RabbitMQ` 配合最自然。
- 注意：这是现有完整文档未覆盖但源码真实存在的能力，新增项目若需要可靠消息优先考虑它。
- 参考：`src/Fake.EntityFrameworkCore.IntegrationEventLog/`

#### `Fake.SqlSugarCore`

- 用途：SqlSugar 与 Fake 的 UoW/仓储集成。
- 主要入口：`FakeSqlSugarCoreModule`、`SugarDbContext<TDbContext>`、`ISugarDbContextProvider<TDbContext>`、`ISqlSugarRepository<TDbContext, TEntity>`。
- 何时使用：项目明确采用 SqlSugar 时。
- 注意：与 EF Core 二选一，不要在同一默认数据模型上混用。
- 参考：`src/Fake.SqlSugarCore/`

#### `Fake.MultiTenant`

- 用途：多租户上下文、租户解析、连接字符串切换。
- 主要入口：`FakeMultiTenantModule`、`ITenantStore`、`ITenantConfigurationProvider`、`ITenantResolveContributor`、`TenantResolveByCurrentUserContributor`。
- 何时使用：不同租户使用不同连接字符串或需要租户上下文解析时。
- 关键事实：模块会把 `IConnectionStringResolver` 替换为 `MultiTenantConnectionStringResolver`。
- 注意：要真正可用，必须自己提供 `ITenantStore`，必要时配置租户解析贡献者。
- 参考：`src/Fake.MultiTenant/`

#### `Fake.Caching`

- 用途：缓存分组标识包。
- 主要入口：`FakeCachingModule`。
- 何时使用：仅当你希望在模块依赖层表达“这是缓存相关能力”时。
- 注意：当前没有统一缓存抽象、没有默认 `ICache`、没有具体实现注册。不要让 AI 基于它生成抽象缓存调用代码。
- 参考：`src/Fake.Caching/`

#### `Fake.Caching.FreeRedis`

- 用途：集成 FreeRedis，提供 `IRedisClient`。
- 主要入口：`FakeCachingFreeRedisModule`、`IRedisInitializer`、`RedisInitializer`、`RedisHelper`。
- 何时使用：项目明确采用 FreeRedis 时。
- 典型配置：提供 `Redis` 配置节，`RedisInitializer` 会读取 `ConnectionStringBuilder[]`。
- 注意：新代码优先通过 DI 注入 `IRedisClient`，不要默认依赖 `RedisHelper.Client` 这种静态入口。
- 参考：`src/Fake.Caching.FreeRedis/`

#### `Fake.Caching.StackExchangeRedis`

- 用途：预留的 StackExchange.Redis 模块位。
- 主要入口：`FakeCachingStackExchangeRedisModule`。
- 何时使用：当前不作为默认方案。
- 注意：模块类存在，但没有具体服务注册与示例接入。
- 参考：`src/Fake.Caching.StackExchangeRedis/`

### 9.3 事件、消息与微服务

#### `Fake.EventBus`

- 用途：本地事件总线。
- 主要入口：`FakeEventBusModule`、`IEventBus`、`ILocalEventBus`、`IEventHandler<TEvent>`、`Event`。
- 何时使用：模块内领域事件、应用层进程内事件分发。
- 关键事实：事件处理器会在服务暴露阶段自动注册对应的 `IEventHandler<TEvent>`。
- 参考：`src/Fake.EventBus/`

#### `Fake.RabbitMQ`

- 用途：RabbitMQ 连接池与通道池。
- 主要入口：`FakeRabbitMqModule`、`FakeRabbitMqOptions`、`IRabbitMqConnectionPool`、`IRabbitMqChannelPool`。
- 何时使用：项目需要基于 RabbitMQ 的分布式事件或自定义消息处理时。
- 典型配置：`RabbitMQ` 配置节，至少准备默认连接。
- 参考：`src/Fake.RabbitMQ/`

#### `Fake.EventBus.RabbitMQ`

- 用途：RabbitMQ 分布式事件总线。
- 主要入口：`FakeEventBusRabbitMqModule`、`IDistributedEventBus`、`IntegrationEvent`、`RabbitMqEventBusOptions`。
- 何时使用：跨服务发布/订阅集成事件。
- 典型配置：搭配 `Fake.RabbitMQ`；按需设置交换机、队列、重试、死信等。
- 注意：当前 `RabbitMqEventBusOptions` 在模块中按整个配置根绑定，建议项目里显式 `Configure<RabbitMqEventBusOptions>`，不要依赖模糊根绑定。
- 参考：`src/Fake.EventBus.RabbitMQ/`

#### `Fake.EventBus.RabbitMQ.OpenTelemetry`

- 用途：预留的 OpenTelemetry / 观测增强包位。
- 状态：占位，不可作为现成能力使用。
- 规则：AI 生成新项目时禁止引用它。
- 参考：`src/Fake.EventBus.RabbitMQ.OpenTelemetry/`

#### `Fake.Consul`

- 用途：Consul 注册、发现、负载均衡、配置中心扩展。
- 主要入口：`FakeConsulModule`、`ConsulHostedService`、`IServiceBalancer`、`AddConsul(this IConfigurationBuilder, string key)`。
- 何时使用：微服务注册发现、Consul KV 热更新、客户端服务解析。
- 典型配置：`Consul`、`Consul:Register`；远程配置可用 `builder.AddConsul(key)`。
- 注意：`FakeConsulModule` 的 `AddConsul(IServiceCollection)` 是内部扩展，业务接入通常通过模块依赖触发。
- 参考：`src/Fake.Consul/`

#### `Fake.Serilog`

- 用途：为 Serilog 提供 FeiShu Sink 与运行时 `IServiceProvider` 桥接。
- 主要入口：`FakeSerilogModule`、`WriteTo.FeiShu(...)`、配置名 `"FeiShu"`、`FeiShuSink`。
- 何时使用：需要把错误日志推送到飞书时。
- 典型配置：宿主仍需 `builder.Host.UseSerilog()`；项目中再依赖 `FakeSerilogModule` 让 Sink 能拿到运行时服务。
- 注意：它不是完整日志框架替代，只是 Serilog 的扩展包；依赖 `FakeCoreModule` 提供的 `IFeiShuNotificationService` 与 `FeiShuNotice` 配置。
- 参考：`src/Fake.Serilog/`、`app/SimpleAdmin.Api/`

### 9.4 Web 与测试

#### `Fake.AspNetCore`

- 用途：Web 集成主包。提供动态 API、控制器约定、Swagger、中间件辅助、HTTP 上下文访问、Web 本地化与异常处理。
- 主要入口：`FakeAspNetCoreModule`、`FakeAspNetCoreMvcOptions`、`ApplicationServices2Controller<TModule>()`、`AddFakeSwaggerGen()`、`UseFakeSwagger()`、`UseFakeExceptionHandling()`、`UseFakeRequestLocalization()`、`AddFakeJwtAuthentication()`、`AddFakeAspNetCoreAuditing()`。
- 何时使用：所有基于 ASP.NET Core 的 Fake API 项目。
- 关键事实：模块会把控制器注册成服务，并自动接入应用服务到控制器的转换约定。
- 注意：若使用应用服务自动暴露，应用服务必须是公开非抽象类型，并位于被扫描模块对应程序集。
- 参考：`src/Fake.AspNetCore/`、`simple/SimpleWebDemo/`

#### `Fake.AspNetCore.Grpc`

- 用途：gRPC 服务端与客户端增强，包含客户端负载均衡、重试、拦截器、SkyWalking 相关扩展。
- 主要入口：`FakeAspNetCoreGrpcModule`、`AddGrpcLoadBalancingClient<TClient>()`、`AddGrpcServer()`。
- 何时使用：项目明确需要 gRPC 且接受当前扩展风格时。
- 注意：模块类本身很轻，主要能力在扩展方法和 balancer/interceptor 实现里；它不是像 `FakeAspNetCore` 那样的重型启动模块。
- 参考：`src/Fake.AspNetCore.Grpc/`

#### `Fake.Testing`

- 用途：应用层集成测试基包。
- 主要入口：`FakeTestingModule`、`ApplicationTest<TStartupModule>`。
- 何时使用：需要在不启动完整 Web Host 的情况下测试模块、服务、仓储协作。
- 关键事实：`ApplicationTest<T>` 默认在 `SetApplicationCreationOptions` 中启用 Autofac。
- 参考：`src/Fake.Testing/`

#### `Fake.AspNetCore.Testing`

- 用途：AspNetCore 集成测试基包。
- 主要入口：`FakeAspNetCoreTestingModule`、`AspNetCoreIntegrationTest<TStartupModule>`、`UseFakeTestServer<TStartupModule>()`、`ITestServerAccessor`。
- 何时使用：需要 `TestServer`、`HttpClient`、完整中间件链测试时。
- 注意：优先用包内基类，而不是自己手拼 Host 生命周期。
- 参考：`src/Fake.AspNetCore.Testing/`

## 10. 面向新项目的默认决策

如果用户只说“基于 Fake 生成一个新项目”，而没有给出更多限制，AI 应采用以下默认决策：

1. Web API 项目，使用 `FakeAutofacModule` + `FakeAspNetCoreModule`。
2. 应用层依赖 `FakeDomainDrivenDesignModule`。
3. ORM 默认选 `Fake.EntityFrameworkCore`。
4. 对象映射默认选 `Fake.ObjectMapping.AutoMapper`。
5. 动态 API 默认通过 `ApplicationServices2Controller<TApplicationModule>()` 暴露。
6. 认证如有需要，默认用 `AddFakeJwtAuthentication()`。
7. 审计默认启用，并在 Web 层调用 `AddFakeAspNetCoreAuditing()`。
8. 本地化默认使用虚拟 JSON 资源。
9. 不默认引入 Consul、RabbitMQ、Outbox/Inbox、多租户、Serilog 飞书 Sink，除非明确需要。

## 11. 已知缺口与保守策略

### 11.1 当前不要默认启用的能力

- `Fake.EventBus.RabbitMQ.OpenTelemetry`
- `Fake.Caching.StackExchangeRedis`
- 仅凭 `Fake.Caching` 生成统一缓存抽象

### 11.2 当前需要业务项目自行补齐的能力

- 权限定义：实现 `IPermissionDefiner`
- 权限持久化：实现 `IPermissionStore`
- 多租户存储：实现 `ITenantStore`
- 真实 Redis 使用策略：按项目决定是否封装业务级缓存服务

### 11.3 当前应避免的误判

- 不要把旧文档里的 API 名当成现行 API。
- 不要因为目录里有包名，就推断它已经有完整可用实现。
- 不要绕过 `ApplicationService` 去直接在控制器里写事务、映射和授权细节。
- 不要假设 Fake 已自带完整缓存抽象、OpenTelemetry 总线增强或完整权限存储实现。

## 12. 生成代码前的最终检查清单

AI 在生成新项目或新模块前，必须检查以下事项：

- 是否已经显式选择了一套 ORM 和一套对象映射 Provider
- 是否已经启用 Autofac
- 是否已经为 Web 项目配置 `ApplicationServices2Controller<TModule>()`
- 是否已经根据需要接入 `UseFakeExceptionHandling()`、`UseAuthentication()`、`UseAuthorization()`
- 是否已经为权限系统补齐 `IPermissionDefiner` 与 `IPermissionStore`
- 是否已经为本地化资源配置 `FakeLocalizationOptions`
- 是否误用了占位包或历史 API 名称

当以上事项无法确认时，优先回查 `src/` 源码，不要依据旧文档或经验猜测。
