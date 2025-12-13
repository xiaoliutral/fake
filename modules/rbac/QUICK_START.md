# RBAC模块快速开始指南

## 1. 模块引用

在你的主项目中引用RBAC模块：

```xml
<ItemGroup>
  <ProjectReference Include="..\..\modules\rbac\Fake.Rbac.Application\Fake.Rbac.Application.csproj" />
  <ProjectReference Include="..\..\modules\rbac\Fake.Rbac.Infrastructure\Fake.Rbac.Infrastructure.csproj" />
</ItemGroup>
```

## 2. 模块依赖配置

在你的主模块中添加依赖：

```csharp
[DependsOn(
    typeof(FakeRbacApplicationModule),
    typeof(FakeRbacInfrastructureModule)
)]
public class YourApplicationModule : FakeModule
{
    // ...
}
```

## 3. 数据库配置

### 3.1 配置DbContext

在你的主DbContext中添加RBAC表：

```csharp
public class YourDbContext : EfCoreDbContext<YourDbContext>
{
    public DbSet<User> Users { get; set; }
    public DbSet<Role> Roles { get; set; }
    public DbSet<Menu> Menus { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // 应用RBAC实体配置
        modelBuilder.ApplyConfiguration(new UserEntityTypeConfiguration());
        modelBuilder.ApplyConfiguration(new RoleEntityTypeConfiguration());
        modelBuilder.ApplyConfiguration(new MenuEntityTypeConfiguration());
        modelBuilder.ApplyConfiguration(new UserRoleEntityTypeConfiguration());
        modelBuilder.ApplyConfiguration(new RolePermissionEntityTypeConfiguration());
        
        base.OnModelCreating(modelBuilder);
    }
}
```

或者直接使用RbacDbContext：

```csharp
services.AddDbContext<RbacDbContext>(options =>
{
    options.UseSqlServer(configuration.GetConnectionString("Default"));
});
```

### 3.2 执行数据库迁移

```bash
# 添加迁移
dotnet ef migrations add InitialRbac -p YourProject.Infrastructure

# 更新数据库
dotnet ef database update -p YourProject.Infrastructure
```

## 4. 初始化数据

在应用启动时执行数据种子：

```csharp
public class Program
{
    public static async Task Main(string[] args)
    {
        var host = CreateHostBuilder(args).Build();
        
        // 执行数据种子
        using (var scope = host.Services.CreateScope())
        {
            var dataSeedService = scope.ServiceProvider
                .GetRequiredService<IDataSeedService>();
            await dataSeedService.SeedAsync();
        }
        
        await host.RunAsync();
    }
}
```

## 5. 使用示例

### 5.1 用户登录

```csharp
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    
    [HttpPost("login")]
    public async Task<UserInfoDto> Login([FromBody] LoginDto input)
    {
        return await _authService.LoginAsync(input.Account, input.Password);
    }
    
    [HttpGet("current")]
    public async Task<UserInfoDto> GetCurrentUser()
    {
        var userId = GetCurrentUserId(); // 从JWT或Session获取
        return await _authService.GetCurrentUserAsync(userId);
    }
}
```

### 5.2 用户管理

```csharp
public class UserController : ControllerBase
{
    private readonly IUserService _userService;
    
    [HttpGet]
    public async Task<PagedResultDto<UserDto>> GetList([FromQuery] UserPagedRequestDto input)
    {
        return await _userService.GetListAsync(input);
    }
    
    [HttpPost]
    public async Task<UserDto> Create([FromBody] UserCreateDto input)
    {
        return await _userService.CreateAsync(input);
    }
    
    [HttpPut("{id}")]
    public async Task<UserDto> Update(Guid id, [FromBody] UserUpdateDto input)
    {
        return await _userService.UpdateAsync(id, input);
    }
    
    [HttpDelete("{id}")]
    public async Task Delete(Guid id)
    {
        await _userService.DeleteAsync(id);
    }
}
```

### 5.3 角色管理

```csharp
public class RoleController : ControllerBase
{
    private readonly IRoleService _roleService;
    
    [HttpGet]
    public async Task<PagedResultDto<RoleDto>> GetList([FromQuery] RolePagedRequestDto input)
    {
        return await _roleService.GetListAsync(input);
    }
    
    [HttpPost]
    public async Task<RoleDto> Create([FromBody] RoleCreateDto input)
    {
        return await _roleService.CreateAsync(input);
    }
    
    [HttpPut("{id}/permissions")]
    public async Task AssignPermissions(Guid id, [FromBody] List<string> permissions)
    {
        await _roleService.AssignPermissionsAsync(id, permissions);
    }
}
```

### 5.4 菜单管理

```csharp
public class MenuController : ControllerBase
{
    private readonly IMenuService _menuService;
    
    [HttpGet("tree")]
    public async Task<List<MenuTreeDto>> GetTree()
    {
        return await _menuService.GetMenuTreeAsync();
    }
    
    [HttpGet("user/{userId}")]
    public async Task<List<MenuTreeDto>> GetUserMenus(Guid userId)
    {
        return await _menuService.GetUserMenusAsync(userId);
    }
    
    [HttpPost]
    public async Task<MenuDto> Create([FromBody] MenuCreateDto input)
    {
        return await _menuService.CreateAsync(input);
    }
}
```

### 5.5 权限验证

```csharp
public class SomeService
{
    private readonly IUserService _userService;
    
    public async Task DoSomething(Guid userId)
    {
        // 检查权限
        if (!await _userService.HasPermissionAsync(userId, "Rbac.Users.Create"))
        {
            throw new UnauthorizedAccessException("没有权限");
        }
        
        // 执行业务逻辑
        // ...
    }
}
```

## 6. 默认账号

系统会自动创建默认管理员账号：

- **账号**：admin
- **密码**：123456
- **角色**：超级管理员（拥有所有权限）

**⚠️ 重要：请在生产环境中立即修改默认密码！**

## 7. API端点

由于框架支持动态API，所有Application Service会自动生成API端点。

假设你的服务接口是 `IUserService`，则会自动生成以下端点：

- `GET /api/rbac/user/{id}` - GetAsync
- `GET /api/rbac/user/list` - GetListAsync
- `POST /api/rbac/user` - CreateAsync
- `PUT /api/rbac/user/{id}` - UpdateAsync
- `DELETE /api/rbac/user/{id}` - DeleteAsync

具体路由规则取决于框架的动态API配置。

## 8. 扩展权限定义

如果需要添加自定义权限：

```csharp
public class MyPermissionDefinitionProvider : IPermissionDefinitionProvider
{
    public List<PermissionDefinition> GetPermissions()
    {
        return new List<PermissionDefinition>
        {
            new("MyModule", "我的模块"),
            new("MyModule.Products", "产品管理", "MyModule"),
            new("MyModule.Products.Query", "查询产品", "MyModule.Products"),
            new("MyModule.Products.Create", "创建产品", "MyModule.Products"),
        };
    }
}
```

在模块中注册：

```csharp
public override void ConfigureServices(ServiceConfigurationContext context)
{
    context.Services.AddSingleton<IPermissionDefinitionProvider, MyPermissionDefinitionProvider>();
}
```

## 9. 常见问题

### Q1: 如何修改密码加密算法？

修改 `User` 实体的 `GeneratePassword` 方法和 `AccountManager` 的密码验证逻辑。

### Q2: 如何添加自定义用户字段？

扩展 `User` 实体，添加新字段，然后更新 `UserEntityTypeConfiguration` 和相关DTO。

### Q3: 如何实现权限缓存？

可以在 `UserService` 的 `GetUserPermissionsAsync` 方法中添加缓存逻辑：

```csharp
public async Task<List<string>> GetUserPermissionsAsync(Guid userId, CancellationToken cancellationToken = default)
{
    var cacheKey = $"user:{userId}:permissions";
    
    // 尝试从缓存获取
    var cached = await _cache.GetAsync<List<string>>(cacheKey);
    if (cached != null)
    {
        return cached;
    }
    
    // 从数据库查询
    var permissions = await GetPermissionsFromDatabase(userId);
    
    // 写入缓存
    await _cache.SetAsync(cacheKey, permissions, TimeSpan.FromMinutes(30));
    
    return permissions;
}
```

### Q4: 如何支持多租户？

User、Role、Menu实体可以实现 `IMultiTenant` 接口，然后启用多租户模块。

## 10. 下一步

- 根据实际需求调整权限定义
- 实现权限验证中间件或特性
- 添加操作日志
- 实现权限缓存
- 集成JWT认证

## 11. 相关文档

- [完整文档](./README.md)
- [实现总结](./IMPLEMENTATION_SUMMARY.md)
- [Fake框架文档](../../docs/Fake框架完整文档.md)
