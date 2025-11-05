# Fake 框架完整文档

> **版本**: 1.0.0  
> **最后更新**: 2025-11-05  
> **目标框架**: .NET 8.0

## 目录

- [1. 框架概述](#1-框架概述)
- [2. 核心架构](#2-核心架构)
- [3. 核心模块详解](#3-核心模块详解)
- [4. 领域驱动设计支持](#4-领域驱动设计支持)
- [5. 应用层支持](#5-应用层支持)
- [6. 基础设施层支持](#6-基础设施层支持)
- [7. 横切关注点](#7-横切关注点)
- [8. 微服务能力](#8-微服务能力)
- [9. 最佳实践](#9-最佳实践)
- [10. 模块清单](#10-模块清单)

---

## 1. 框架概述

### 1.1 设计理念

Fake 是一个基于 **.NET 8.0** 的模块化应用开发框架，致力于探索 Web 应用程序编程的最佳实践。框架的核心设计理念包括：

- **模块化**：将复杂系统划分为独立的模块，提高可维护性和可扩展性
- **依赖注入**：贯穿始终的 DI 设计模式，基于 Autofac 容器
- **领域驱动设计（DDD）**：完整的 DDD 战术模式支持
- **约定优于配置**：通过约定减少配置工作量
- **动态代理（AOP）**：基于 Castle.Core 的拦截器机制

### 1.2 主要特性

#### 核心能力
- ✅ 模块化架构
- ✅ 自动依赖注入（Transient、Scoped、Singleton）
- ✅ 属性注入
- ✅ 动态代理和拦截器

#### 授权与安全
- ✅ 基于角色的访问控制（RBAC）
- ✅ 基于策略的动态授权
- ✅ 自定义认证方案

#### 审计日志
- ✅ 请求审计
- ✅ 方法审计
- ✅ 实体审计
- ✅ 实体变更审计

#### 本地化
- ✅ 多语言支持（JSON资源文件）
- ⏳ 远程动态数据（计划中）

#### 文件系统
- ✅ 虚拟文件系统
- ✅ wwwroot 物理文件系统

#### 工作单元
- ✅ 自动事务管理
- ✅ EF Core 集成

#### 测试
- ✅ 集成测试支持
- ✅ AspNetCore Host 测试

#### 对象映射
- ✅ AutoMapper 集成
- ✅ Mapster 集成

#### 事件总线
- ✅ 本地事件总线
- ✅ RabbitMQ 分布式事件总线

#### 领域驱动设计
- ✅ 实体和聚合根
- ✅ 值对象和枚举
- ✅ 仓储模式
- ✅ 领域事件
- ✅ 领域服务
- ✅ 实体审计和乐观锁

#### 微服务能力
- ✅ Consul 服务发现与注册
- ✅ 客户端负载均衡
- ✅ KV 配置热更新
- ✅ Grpc 客户端负载均衡
- ✅ RabbitMQ 分布式事件总线
- ⏳ SkyWalking 分布式链路追踪（计划中）

---

## 2. 核心架构

### 2.1 框架分层

```
┌────────────────────────────────────────────┐
│         Presentation Layer                 │  HTTP API / Grpc / Console
│  (Fake.AspNetCore / Fake.AspNetCore.Grpc) │
├────────────────────────────────────────────┤
│         Application Layer                  │  Application Services
│    (Your.Application + DDD Support)        │  DTOs / AutoMapper
├────────────────────────────────────────────┤
│         Domain Layer                       │  Domain Model
│    (Your.Domain + Fake.DDD)               │  Entities / Value Objects
│                                            │  Domain Services / Events
├────────────────────────────────────────────┤
│         Infrastructure Layer               │  Data Access / External
│    (Your.Infrastructure + Fake.EFCore)    │  Repositories / Integration
└────────────────────────────────────────────┘
         ↓ Cross-Cutting Concerns ↓
┌────────────────────────────────────────────┐
│  Fake.Authorization / Fake.Auditing        │  Authorization / Auditing
│  Fake.UnitOfWork / Fake.Caching           │  UoW / Caching
│  Fake.EventBus / Fake.Localization        │  Event Bus / i18n
└────────────────────────────────────────────┘
         ↓ Foundation ↓
┌────────────────────────────────────────────┐
│              Fake.Core                     │  Modularity / DI
│         (Core Infrastructure)              │  Dynamic Proxy / JSON
└────────────────────────────────────────────┘
```

### 2.2 FakeApplication 生命周期

```
┌─────────────────────────────────────────┐
│  1. 创建 FakeApplication                 │
│     - 设置启动模块                        │
│     - 创建服务容器                        │
│     - 添加核心服务                        │
│     - 加载配置（appsettings.json）       │
│     - 添加日志                           │
└─────────────────────────────────────────┘
                    ↓
┌─────────────────────────────────────────┐
│  2. 加载模块（Topology Sort）            │
│     - 扫描模块依赖关系                    │
│     - 拓扑排序（避免循环依赖）            │
│     - 生成模块加载链                      │
└─────────────────────────────────────────┘
                    ↓
┌─────────────────────────────────────────┐
│  3. ConfigureServices（服务配置阶段）    │
│     ├─ PreConfigureServices              │
│     ├─ 自动服务注册（每个模块的程序集）   │
│     ├─ ConfigureServices                 │
│     └─ PostConfigureServices             │
└─────────────────────────────────────────┘
                    ↓
┌─────────────────────────────────────────┐
│  4. InitializeApplication（初始化阶段）  │
│     - 构建 ServiceProvider               │
│     ├─ PreConfigureApplication           │
│     ├─ ConfigureApplication              │
│     └─ PostConfigureApplication          │
└─────────────────────────────────────────┘
                    ↓
┌─────────────────────────────────────────┐
│  5. 应用运行                             │
│     - 处理请求                           │
│     - 执行业务逻辑                        │
└─────────────────────────────────────────┘
                    ↓
┌─────────────────────────────────────────┐
│  6. Shutdown（关闭阶段）                 │
│     - 清理资源                           │
│     - 释放服务                           │
└─────────────────────────────────────────┘
```

### 2.3 模块系统

#### 模块定义

```csharp
[DependsOn(typeof(FakeDomainDrivenDesignModule))]
public class YourModule : FakeModule
{
    public override void PreConfigureServices(ServiceConfigurationContext context)
    {
        // 服务配置前的准备工作
        // 例如：注册拦截器、添加服务暴露动作等
    }

    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        // 主要的服务注册逻辑
        context.Services.AddSingleton<IYourService, YourService>();
    }

    public override void PostConfigureServices(ServiceConfigurationContext context)
    {
        // 服务配置后的收尾工作
    }

    public override void PreConfigureApplication(ApplicationConfigureContext context)
    {
        // 应用配置前的准备工作
    }

    public override void ConfigureApplication(ApplicationConfigureContext context)
    {
        // 主要的应用配置逻辑（如中间件配置）
    }

    public override void PostConfigureApplication(ApplicationConfigureContext context)
    {
        // 应用配置后的收尾工作
    }

    public override void Shutdown(ApplicationShutdownContext context)
    {
        // 模块关闭时的清理工作
    }
}
```

#### 模块依赖

```csharp
// 单个依赖
[DependsOn(typeof(FakeDomainDrivenDesignModule))]
public class YourModule : FakeModule { }

// 多个依赖
[DependsOn(
    typeof(FakeDomainDrivenDesignModule),
    typeof(FakeEntityFrameworkCoreModule),
    typeof(FakeAutoMapperModule)
)]
public class YourModule : FakeModule { }
```

#### 跳过自动服务注册

```csharp
public class YourModule : FakeModule
{
    // 设置为 true 可跳过 Fake 的自动服务注册
    public override bool SkipServiceRegistration => true;
}
```

---

## 3. 核心模块详解

### 3.1 Fake.Core - 核心模块

**职责**：提供框架的基础设施和核心能力

#### 主要功能

##### 3.1.1 模块化系统

- **FakeModule**: 模块基类
- **IModuleLoader**: 模块加载器（支持拓扑排序）
- **DependsOnAttribute**: 模块依赖标记

##### 3.1.2 依赖注入

**服务生命周期标记**:
```csharp
public interface ITransientDependency { }  // 瞬时
public interface IScopedDependency { }     // 作用域
public interface ISingletonDependency { }  // 单例
```

**服务注册**:
```csharp
// 自动注册（实现生命周期接口即可）
public class MyService : IMyService, ITransientDependency { }

// 手动暴露服务
[ExposeServices(typeof(IMyService), typeof(IMyOtherService))]
public class MyService : IScopedDependency { }

// 禁用服务注册
[DisableServiceRegistration]
public class MyService : IMyService { }
```

**服务注册切面**:
```csharp
// 在 PreConfigureServices 中添加自定义注册器
context.Services.AddServiceRegistrar(new CustomServiceRegistrar());

// 服务暴露切面
context.Services.OnServiceExposing(ctx =>
{
    // 自定义暴露逻辑
    if (ctx.ImplementationType.IsGenericType)
    {
        ctx.ExposedServiceTypes.Add(someType);
    }
});

// 服务注册后切面（需要 Autofac 模块）
context.Services.OnRegistered(ctx =>
{
    // 添加拦截器
    if (ShouldIntercept(ctx.ImplementationType))
    {
        ctx.Interceptors.TryAdd<MyInterceptor>();
    }
});
```

##### 3.1.3 动态代理

```csharp
public interface IFakeInterceptor
{
    Task InterceptAsync(IFakeMethodInvocation invocation);
}

public class MyInterceptor : IFakeInterceptor
{
    public async Task InterceptAsync(IFakeMethodInvocation invocation)
    {
        // 方法执行前
        Console.WriteLine("Before method execution");

        // 执行方法
        await invocation.ProceedAsync();

        // 方法执行后
        Console.WriteLine("After method execution");
    }
}
```

##### 3.1.4 配置系统

```csharp
// appsettings.json
{
  "ApplicationName": "MyApp",
  "Logging": {
    "LogLevel": {
      "Default": "Information"
    }
  }
}

// 程序中访问配置
var configuration = app.ServiceProvider.GetRequiredService<IConfiguration>();
var appName = configuration["ApplicationName"];
```

##### 3.1.5 ID 生成器

```csharp
// Guid 生成器
var guidGenerator = serviceProvider.GetRequiredService<IIdGenerator<Guid>>();
var id = guidGenerator.Generate(); // Sequential Guid

// 雪花 ID 生成器
var snowflakeGenerator = serviceProvider.GetRequiredService<IIdGenerator<long>>();
var id = snowflakeGenerator.Generate();

// 配置雪花 ID
services.Configure<SnowflakeIdGeneratorOptions>(options =>
{
    options.WorkerId = 1;
    options.DatacenterId = 1;
});
```

##### 3.1.6 JSON 序列化

基于 System.Text.Json，提供了增强配置：

```csharp
// 默认配置
- 属性名不区分大小写
- 驼峰命名
- 允许从字符串读取数字
- 允许注释和尾部逗号
- DateTime 自动转换
- Long 类型安全转换
- Boolean 灵活转换

// 自定义配置
services.Configure<JsonSerializerOptions>(options =>
{
    options.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
});
```

##### 3.1.7 其他工具

- **时钟服务**: `IFakeClock` - 统一的时间获取（支持时区）
- **辅助类**: MD5Helper, RandomHelper, ReflectionHelper, TypeHelper
- **线程同步**: 异步锁、信号量等
- **数据过滤**: `IDataFilter<T>` - 支持软删除、多租户等过滤

---

### 3.2 Fake.DomainDrivenDesign - DDD 支持

#### 3.2.1 实体（Entity）

```csharp
// 基础实体
public class Product : Entity<Guid>
{
    public string Name { get; set; }
    public decimal Price { get; set; }
}

// 审计实体
public class Order : FullAuditedAggregateRoot<Guid>
{
    public string OrderNo { get; set; }
    public DateTime OrderDate { get; set; }
    // 自动包含: CreatedAt, CreatedBy, UpdatedAt, UpdatedBy, DeletedAt, DeletedBy, IsDeleted
}
```

**审计实体类型**:
- `CreateAuditedEntity`: 创建审计
- `UpdateAuditedEntity`: 更新审计
- `DeleteAuditedEntity`: 删除审计（软删除）
- `FullAuditedEntity`: 完整审计

#### 3.2.2 聚合根（Aggregate Root）

```csharp
public class Order : FullAuditedAggregateRoot<Guid>
{
    public string OrderNo { get; private set; }
    
    private readonly List<OrderItem> _items = new();
    public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();

    public Order(string orderNo)
    {
        OrderNo = orderNo;
    }

    public void AddItem(Product product, int quantity)
    {
        var item = _items.FirstOrDefault(x => x.ProductId == product.Id);
        if (item != null)
        {
            item.IncreaseQuantity(quantity);
        }
        else
        {
            _items.Add(new OrderItem(Id, product.Id, quantity, product.Price));
        }
    }

    public void RemoveItem(Guid productId)
    {
        var item = _items.FirstOrDefault(x => x.ProductId == productId);
        if (item != null)
        {
            _items.Remove(item);
        }
    }
}
```

#### 3.2.3 值对象（Value Object）

```csharp
public class Address : ValueObject
{
    public string Street { get; private set; }
    public string City { get; private set; }
    public string ZipCode { get; private set; }

    public Address(string street, string city, string zipCode)
    {
        Street = street;
        City = city;
        ZipCode = zipCode;
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Street;
        yield return City;
        yield return ZipCode;
    }
}
```

#### 3.2.4 枚举（Enumeration）

```csharp
public class OrderStatus : Enumeration
{
    public static OrderStatus Pending = new(1, nameof(Pending));
    public static OrderStatus Confirmed = new(2, nameof(Confirmed));
    public static OrderStatus Shipped = new(3, nameof(Shipped));
    public static OrderStatus Completed = new(4, nameof(Completed));

    public OrderStatus(int id, string name) : base(id, name)
    {
    }
}

// 使用
var status = OrderStatus.Pending;
var allStatuses = OrderStatus.GetAll<OrderStatus>();
```

#### 3.2.5 仓储（Repository）

```csharp
public interface IOrderRepository : IRepository<Order>
{
    Task<Order?> FindByOrderNoAsync(string orderNo);
    Task<List<Order>> GetUserOrdersAsync(Guid userId);
}

// IRepository 提供的基础方法:
- FirstAsync / FirstOrDefaultAsync
- GetListAsync / GetPagedListAsync
- CountAsync / AnyAsync
- InsertAsync / InsertRangeAsync
- UpdateAsync / UpdateRangeAsync
- DeleteAsync / DeleteRangeAsync
```

#### 3.2.6 领域事件

```csharp
// 定义领域事件
public class OrderCreatedEvent : DomainEvent
{
    public Guid OrderId { get; set; }
    public string OrderNo { get; set; }
}

// 在聚合根中添加事件
public class Order : FullAuditedAggregateRoot<Guid>, IHasDomainEvent
{
    private readonly List<DomainEvent> _domainEvents = new();
    public IReadOnlyCollection<DomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    public void AddDomainEvent(DomainEvent eventItem)
    {
        _domainEvents.Add(eventItem);
    }

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }

    public Order(string orderNo)
    {
        OrderNo = orderNo;
        AddDomainEvent(new OrderCreatedEvent { OrderId = Id, OrderNo = orderNo });
    }
}
```

#### 3.2.7 领域服务

```csharp
public class OrderManager : DomainService
{
    private readonly IOrderRepository _orderRepository;

    public OrderManager(IOrderRepository orderRepository)
    {
        _orderRepository = orderRepository;
    }

    public async Task<Order> CreateOrderAsync(string orderNo, List<OrderItem> items)
    {
        // 业务规则验证
        await ValidateOrderNoUniqueAsync(orderNo);

        var order = new Order(orderNo);
        foreach (var item in items)
        {
            order.AddItem(item.Product, item.Quantity);
        }

        await _orderRepository.InsertAsync(order);
        return order;
    }

    private async Task ValidateOrderNoUniqueAsync(string orderNo)
    {
        var exists = await _orderRepository.FindByOrderNoAsync(orderNo);
        if (exists != null)
        {
            throw new DomainException($"订单号已存在: {orderNo}");
        }
    }
}
```

#### 3.2.8 领域异常

```csharp
// 领域异常
public class OrderNotFoundException : DomainException
{
    public OrderNotFoundException(Guid orderId) 
        : base($"订单不存在: {orderId}")
    {
    }
}

// 业务异常
public class InsufficientStockException : BusinessException
{
    public InsufficientStockException(string productName, int requested, int available)
        : base($"库存不足: {productName}, 请求: {requested}, 可用: {available}")
    {
    }
}
```

---

## 4. 领域驱动设计支持

### 4.1 DDD 分层架构

```
┌─────────────────────────────────────────┐
│        User Interface Layer             │  Controllers / Pages
│  (Presentation)                         │
├─────────────────────────────────────────┤
│        Application Layer                │  Application Services
│  (Orchestration)                        │  DTOs / Mappers
├─────────────────────────────────────────┤
│        Domain Layer                     │  Domain Model
│  (Business Logic)                       │  - Entities
│                                         │  - Value Objects
│                                         │  - Domain Services
│                                         │  - Domain Events
│                                         │  - Specifications
├─────────────────────────────────────────┤
│        Infrastructure Layer             │  Technical Concerns
│  (Technical Services)                   │  - Repositories
│                                         │  - External Services
│                                         │  - Caching
└─────────────────────────────────────────┘
```

### 4.2 应用服务层

```csharp
public class OrderAppService : ApplicationService
{
    private readonly IOrderRepository _orderRepository;
    private readonly OrderManager _orderManager;
    private readonly IObjectMapper _objectMapper;

    public OrderAppService(
        IOrderRepository orderRepository,
        OrderManager orderManager,
        IObjectMapper objectMapper)
    {
        _orderRepository = orderRepository;
        _orderManager = orderManager;
        _objectMapper = objectMapper;
    }

    public async Task<OrderDto> CreateOrderAsync(CreateOrderDto input)
    {
        // 应用服务只做协调，业务逻辑在领域层
        var order = await _orderManager.CreateOrderAsync(
            input.OrderNo, 
            input.Items);

        return _objectMapper.Map<Order, OrderDto>(order);
    }

    public async Task<OrderDto> GetOrderAsync(Guid id)
    {
        var order = await _orderRepository.GetAsync(id);
        return _objectMapper.Map<Order, OrderDto>(order);
    }
}
```

---

## 5. 应用层支持

### 5.1 ApplicationService 基类

```csharp
public abstract class ApplicationService : IApplicationService
{
    // 提供常用服务的属性注入
    public IServiceProvider ServiceProvider { get; set; }
    public ILogger Logger { get; set; }
    public IObjectMapper ObjectMapper { get; set; }
}
```

### 5.2 DTOs

```csharp
// 列表结果
public class ListResult<T>
{
    public List<T> Items { get; set; }
}

// 分页结果
public class PagedResult<T> : ListResult<T>
{
    public long Total { get; set; }
}

// 分页请求
public class PagedRequest
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? OrderBy { get; set; }
    public bool Descending { get; set; }
}
```

### 5.3 业务异常

```csharp
public class BusinessException : FakeException
{
    public string Code { get; set; }
    
    public BusinessException(string message, string code = null) 
        : base(message)
    {
        Code = code;
    }
}

// 使用
if (stock < quantity)
{
    throw new BusinessException("库存不足", "INSUFFICIENT_STOCK");
}
```

---

## 6. 基础设施层支持

### 6.1 Entity Framework Core

**模块**: `Fake.EntityFrameworkCore`

```csharp
[DependsOn(typeof(FakeEntityFrameworkCoreModule))]
public class YourInfrastructureModule : FakeModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddDbContext<YourDbContext>(options =>
        {
            options.UseSqlServer(configuration.GetConnectionString("Default"));
        });
    }
}

// DbContext
public class YourDbContext : EfCoreDbContext<YourDbContext>
{
    public DbSet<Order> Orders { get; set; }
    public DbSet<Product> Products { get; set; }

    public YourDbContext(DbContextOptions<YourDbContext> options) 
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        modelBuilder.ApplyConfiguration(new OrderConfiguration());
    }
}

// 仓储实现
public class OrderRepository : EfCoreRepository<YourDbContext, Order>, IOrderRepository
{
    public OrderRepository(IDbContextProvider<YourDbContext> dbContextProvider) 
        : base(dbContextProvider)
    {
    }

    public async Task<Order?> FindByOrderNoAsync(string orderNo)
    {
        var dbContext = await GetDbContextAsync();
        return await dbContext.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.OrderNo == orderNo);
    }
}
```

### 6.2 SqlSugar

**模块**: `Fake.SqlSugarCore`

```csharp
[DependsOn(typeof(FakeSqlSugarCoreModule))]
public class YourInfrastructureModule : FakeModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddSqlSugar(options =>
        {
            options.ConnectionString = configuration.GetConnectionString("Default");
            options.DbType = DbType.SqlServer;
        });
    }
}
```

---

## 7. 横切关注点

### 7.1 授权（Authorization）

**模块**: `Fake.Authorization`

```csharp
// 定义权限
public class YourPermissionDefinitionProvider : IPermissionDefinitionProvider
{
    public void Define(IPermissionDefinitionContext context)
    {
        var group = context.AddGroup("YourModule", "Your Module");
        
        var orders = group.AddPermission("Orders", "Order Management");
        orders.AddChild("Orders.Create", "Create Order");
        orders.AddChild("Orders.Update", "Update Order");
        orders.AddChild("Orders.Delete", "Delete Order");
    }
}

// 使用权限
[Authorize(Permission = "Orders.Create")]
public async Task<OrderDto> CreateOrderAsync(CreateOrderDto input)
{
    // ...
}

// 手动检查权限
if (!await _permissionChecker.IsGrantedAsync("Orders.Delete"))
{
    throw new FakeAuthorizationException("没有删除权限");
}
```

### 7.2 审计（Auditing）

**模块**: `Fake.Auditing`

```csharp
// 自动审计（继承审计实体即可）
public class Order : FullAuditedAggregateRoot<Guid>
{
    // CreatedAt, CreatedBy, UpdatedAt, UpdatedBy 自动记录
}

// 方法审计
[Audited]
public async Task<OrderDto> CreateOrderAsync(CreateOrderDto input)
{
    // 方法执行会被记录
}

// 禁用审计
[DisableAuditing]
public async Task SensitiveOperationAsync()
{
    // 不会被审计
}
```

### 7.3 工作单元（Unit of Work）

**模块**: `Fake.UnitOfWork`

```csharp
// 自动工作单元（应用服务默认启用）
public class OrderAppService : ApplicationService
{
    [UnitOfWork]  // 可选，默认已启用
    public async Task<OrderDto> CreateOrderAsync(CreateOrderDto input)
    {
        // 方法内的所有数据库操作会在一个事务中
        var order = await _orderRepository.InsertAsync(new Order());
        var payment = await _paymentRepository.InsertAsync(new Payment());
        
        // 方法执行成功后自动提交事务
        // 抛出异常自动回滚
    }
}

// 禁用工作单元
[DisableUnitOfWork]
public async Task QueryOrderAsync(Guid id)
{
    // 只读操作可以禁用工作单元
}

// 手动控制工作单元
public async Task ComplexOperationAsync()
{
    using (var uow = _unitOfWorkManager.Begin())
    {
        try
        {
            await _orderRepository.InsertAsync(order);
            await _paymentRepository.InsertAsync(payment);
            
            await uow.CompleteAsync();
        }
        catch
        {
            await uow.RollbackAsync();
            throw;
        }
    }
}
```

### 7.4 对象映射（Object Mapping）

**模块**: `Fake.ObjectMapping.AutoMapper`

```csharp
// AutoMapper Profile
public class YourMappingProfile : Profile
{
    public YourMappingProfile()
    {
        CreateMap<Order, OrderDto>();
        CreateMap<OrderItem, OrderItemDto>();
        CreateMap<CreateOrderDto, Order>();
    }
}

// 模块配置
[DependsOn(typeof(FakeAutoMapperModule))]
public class YourApplicationModule : FakeModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.Configure<FakeAutoMapperOptions>(options =>
        {
            options.AddProfile<YourMappingProfile>(validate: false);
        });
    }
}

// 使用
public class OrderAppService : ApplicationService
{
    public async Task<OrderDto> GetOrderAsync(Guid id)
    {
        var order = await _orderRepository.GetAsync(id);
        return ObjectMapper.Map<Order, OrderDto>(order);
    }
}
```

### 7.5 缓存（Caching）

**模块**: `Fake.Caching.FreeRedis` / `Fake.Caching.StackExchangeRedis`

```csharp
// 配置 Redis
[DependsOn(typeof(FakeCachingFreeRedisModule))]
public class YourModule : FakeModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddFreeRedis(options =>
        {
            options.ConnectionString = "localhost:6379";
        });
    }
}

// 使用缓存
public class OrderAppService : ApplicationService
{
    private readonly IDistributedCache _cache;

    public async Task<OrderDto> GetOrderAsync(Guid id)
    {
        var cacheKey = $"order:{id}";
        
        // 尝试从缓存获取
        var cached = await _cache.GetStringAsync(cacheKey);
        if (cached != null)
        {
            return JsonSerializer.Deserialize<OrderDto>(cached);
        }

        // 从数据库获取
        var order = await _orderRepository.GetAsync(id);
        var dto = ObjectMapper.Map<Order, OrderDto>(order);

        // 写入缓存
        await _cache.SetStringAsync(cacheKey, 
            JsonSerializer.Serialize(dto),
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
            });

        return dto;
    }
}
```

### 7.6 本地化（Localization）

**模块**: `Fake.Localization`

```csharp
// 配置本地化
public override void ConfigureServices(ServiceConfigurationContext context)
{
    context.Services.Configure<FakeLocalizationOptions>(options =>
    {
        options.Resources
            .Add<YourResource>("zh")
            .LoadVirtualJson("/Localization");
    });
}

// JSON 资源文件 (/Localization/zh.json)
{
  "WelcomeMessage": "欢迎使用",
  "OrderNotFound": "订单不存在"
}

// 使用
public class OrderAppService : ApplicationService
{
    private readonly IStringLocalizer<YourResource> _localizer;

    public async Task<string> GetWelcomeMessageAsync()
    {
        return _localizer["WelcomeMessage"];
    }
}
```

### 7.7 事件总线（Event Bus）

**本地事件总线**:

```csharp
// 定义事件
public class OrderCreatedEvent
{
    public Guid OrderId { get; set; }
    public string OrderNo { get; set; }
}

// 事件处理器
public class OrderCreatedEventHandler : ILocalEventHandler<OrderCreatedEvent>
{
    public async Task HandleEventAsync(OrderCreatedEvent eventData)
    {
        // 处理订单创建事件
        Console.WriteLine($"Order created: {eventData.OrderNo}");
    }
}

// 发布事件
public class OrderAppService : ApplicationService
{
    private readonly ILocalEventBus _eventBus;

    public async Task<OrderDto> CreateOrderAsync(CreateOrderDto input)
    {
        var order = await _orderRepository.InsertAsync(new Order());
        
        await _eventBus.PublishAsync(new OrderCreatedEvent
        {
            OrderId = order.Id,
            OrderNo = order.OrderNo
        });

        return ObjectMapper.Map<Order, OrderDto>(order);
    }
}
```

**分布式事件总线（RabbitMQ）**:

```csharp
[DependsOn(typeof(FakeEventBusRabbitMqModule))]
public class YourModule : FakeModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddRabbitMq(options =>
        {
            options.HostName = "localhost";
            options.UserName = "guest";
            options.Password = "guest";
        });
    }
}

// 分布式事件处理器
public class OrderCreatedIntegrationEventHandler 
    : IDistributedEventHandler<OrderCreatedIntegrationEvent>
{
    public async Task HandleEventAsync(OrderCreatedIntegrationEvent eventData)
    {
        // 处理来自其他服务的事件
    }
}
```

---

## 8. 微服务能力

### 8.1 服务发现与注册（Consul）

**模块**: `Fake.Consul`

```csharp
[DependsOn(typeof(FakeConsulModule))]
public class YourModule : FakeModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddConsul(options =>
        {
            options.ConsulAddress = "http://localhost:8500";
            options.ServiceName = "order-service";
            options.ServiceAddress = "http://localhost:5000";
            options.HealthCheckInterval = TimeSpan.FromSeconds(10);
        });
    }
}
```

### 8.2 gRPC 支持

**模块**: `Fake.AspNetCore.Grpc`

```csharp
[DependsOn(typeof(FakeAspNetCoreGrpcModule))]
public class YourModule : FakeModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddGrpc();
    }

    public override void ConfigureApplication(ApplicationConfigureContext context)
    {
        var app = context.GetApplicationBuilder();
        
        app.MapGrpcService<OrderGrpcService>();
    }
}
```

### 8.3 RabbitMQ 消息队列

**模块**: `Fake.EventBus.RabbitMQ`

```csharp
// 配置 RabbitMQ
services.Configure<RabbitMqOptions>(options =>
{
    options.HostName = "localhost";
    options.UserName = "guest";
    options.Password = "guest";
    options.Port = 5672;
});

// 发布消息
await _distributedEventBus.PublishAsync(new OrderCreatedIntegrationEvent
{
    OrderId = order.Id,
    OrderNo = order.OrderNo
});
```

---

## 9. 最佳实践

### 9.1 项目结构

```
YourProject/
├── src/
│   ├── YourProject.Domain/           # 领域层
│   │   ├── Entities/
│   │   ├── ValueObjects/
│   │   ├── DomainServices/
│   │   ├── Repositories/
│   │   └── YourProjectDomainModule.cs
│   │
│   ├── YourProject.Application/      # 应用层
│   │   ├── Services/
│   │   ├── Dtos/
│   │   ├── AutoMapper/
│   │   └── YourProjectApplicationModule.cs
│   │
│   ├── YourProject.Infrastructure/   # 基础设施层
│   │   ├── EntityConfigurations/
│   │   ├── Repositories/
│   │   ├── YourDbContext.cs
│   │   └── YourProjectInfrastructureModule.cs
│   │
│   └── YourProject.HttpApi/          # HTTP API 层
│       ├── Controllers/
│       ├── Program.cs
│       └── YourProjectHttpApiModule.cs
│
├── tests/
│   ├── YourProject.Domain.Tests/
│   ├── YourProject.Application.Tests/
│   └── YourProject.HttpApi.Tests/
│
└── YourProject.sln
```

### 9.2 领域层最佳实践

1. **聚合根边界明确**：一个聚合根管理一组相关实体
2. **业务逻辑在领域层**：不要在应用服务层写业务逻辑
3. **使用值对象**：用值对象封装复杂的数据结构
4. **领域事件解耦**：使用领域事件在聚合间通信
5. **仓储只返回聚合根**：不要返回实体的一部分

### 9.3 应用层最佳实践

1. **应用服务只做协调**：调用领域服务和仓储
2. **使用 DTO 传输数据**：不要直接返回实体
3. **一个方法一个事务**：利用工作单元模式
4. **处理异常转换**：将领域异常转换为应用异常

### 9.4 基础设施层最佳实践

1. **仓储实现查询优化**：使用 Include、AsNoTracking 等
2. **避免 N+1 查询**：合理使用 Eager Loading
3. **数据库迁移管理**：使用 EF Core Migrations
4. **配置与代码分离**：使用 IEntityTypeConfiguration

---

## 10. 模块清单

### 10.1 核心模块

| 模块 | 说明 | 依赖 |
|------|------|------|
| **Fake.Core** | 核心基础设施 | - |
| **Fake.DomainDrivenDesign** | DDD 支持 | Fake.Core |
| **Fake.Castle** | Castle 动态代理 | Fake.Core |
| **Fake.Autofac** | Autofac 容器 | Fake.Castle |

### 10.2 应用层模块

| 模块 | 说明 | 依赖 |
|------|------|------|
| **Fake.ObjectMapping** | 对象映射抽象 | Fake.Core |
| **Fake.ObjectMapping.AutoMapper** | AutoMapper 实现 | Fake.ObjectMapping |
| **Fake.ObjectMapping.Mapster** | Mapster 实现 | Fake.ObjectMapping |

### 10.3 基础设施模块

| 模块 | 说明 | 依赖 |
|------|------|------|
| **Fake.EntityFrameworkCore** | EF Core 支持 | Fake.DDD |
| **Fake.SqlSugarCore** | SqlSugar 支持 | Fake.DDD |
| **Fake.Caching** | 缓存抽象 | Fake.Core |
| **Fake.Caching.FreeRedis** | FreeRedis 实现 | Fake.Caching |
| **Fake.Caching.StackExchangeRedis** | StackExchange.Redis 实现 | Fake.Caching |

### 10.4 横切关注点模块

| 模块 | 说明 | 依赖 |
|------|------|------|
| **Fake.Authorization** | 授权 | Fake.Security |
| **Fake.Auditing** | 审计 | Fake.Core |
| **Fake.UnitOfWork** | 工作单元 | Fake.Core |
| **Fake.Localization** | 本地化 | Fake.VirtualFileSystem |
| **Fake.VirtualFileSystem** | 虚拟文件系统 | Fake.Core |
| **Fake.Security** | 安全 | Fake.Core |
| **Fake.MultiTenant** | 多租户 | Fake.Core |

### 10.5 事件模块

| 模块 | 说明 | 依赖 |
|------|------|------|
| **Fake.EventBus** | 事件总线 | Fake.Core |
| **Fake.EventBus.RabbitMQ** | RabbitMQ 实现 | Fake.EventBus, Fake.RabbitMQ |
| **Fake.RabbitMQ** | RabbitMQ 基础 | Fake.Core |

### 10.6 Web 模块

| 模块 | 说明 | 依赖 |
|------|------|------|
| **Fake.AspNetCore** | AspNetCore 支持 | Fake.Core |
| **Fake.AspNetCore.Grpc** | Grpc 支持 | Fake.AspNetCore |
| **Fake.AspNetCore.Testing** | 集成测试 | Fake.AspNetCore |

### 10.7 微服务模块

| 模块 | 说明 | 依赖 |
|------|------|------|
| **Fake.Consul** | Consul 服务发现 | Fake.Core |

### 10.8 测试模块

| 模块 | 说明 | 依赖 |
|------|------|------|
| **Fake.Testing** | 测试基础设施 | Fake.Core |

---

## 11. 快速开始

### 11.1 控制台应用

```csharp
using Fake;

static void Main(string[] args)
{
    using var application = FakeApplicationFactory.Create<YourModule>();
    application.InitializeApplication();

    var logger = application.ServiceProvider
        .GetRequiredService<ILogger<Program>>();
    logger.LogInformation("Application started!");

    // 业务逻辑
}

public class YourModule : FakeModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        // 配置服务
    }
}
```

### 11.2 Web 应用

```csharp
var builder = WebApplication.CreateBuilder(args);

// 添加 Fake 应用
builder.Services.AddFakeApplication<YourWebModule>();

var app = builder.Build();

// 初始化 Fake 应用
app.InitializeApplication();

app.MapGet("/", () => "Hello Fake!");
app.Run();

[DependsOn(typeof(FakeAspNetCoreModule))]
public class YourWebModule : FakeModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        // 配置服务
    }

    public override void ConfigureApplication(ApplicationConfigureContext context)
    {
        var app = context.GetApplicationBuilder();
        
        // 配置中间件
        app.UseRouting();
        app.UseAuthentication();
        app.UseAuthorization();
    }
}
```

---

## 12. 参考资料

### 12.1 官方文档

- [ABP Framework](https://docs.abp.io/) - 主要参考项目
- [MediatR](https://github.com/jbogard/MediatR) - CQRS 模式参考
- [eShopOnContainers](https://github.com/dotnet-architecture/eShopOnContainers) - 微服务架构参考

### 12.2 相关技术

- [Autofac](https://autofac.org/) - DI 容器
- [Castle.Core](http://www.castleproject.org/) - 动态代理
- [AutoMapper](https://automapper.org/) - 对象映射
- [Entity Framework Core](https://docs.microsoft.com/ef/core/) - ORM
- [RabbitMQ](https://www.rabbitmq.com/) - 消息队列
- [Consul](https://www.consul.io/) - 服务发现

---

## 附录

### A. 常见问题

**Q: Fake 和 ABP 的区别？**

A: Fake 是受 ABP 启发的轻量级框架，主要区别：
- 更简化的模块系统
- 去除了一些 ABP 的复杂抽象
- 专注于 DDD 和微服务场景
- 更灵活的扩展机制

**Q: 如何选择对象映射工具？**

A: 
- AutoMapper：功能强大，生态完善，配置复杂
- Mapster：性能优秀，配置简单，功能相对少

**Q: 如何处理分布式事务？**

A:
- 使用 Saga 模式
- 使用分布式事件总线实现最终一致性
- 考虑使用 DTM 等分布式事务管理器

**Q: 如何实现多租户？**

A:
- 使用 `Fake.MultiTenant` 模块
- 实现 `IMultiTenant` 接口
- 配置租户解析策略

---

**文档结束** 🎉

如有问题或建议，请提交 Issue 或 Pull Request。

