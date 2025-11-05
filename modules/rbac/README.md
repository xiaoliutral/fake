# RBAC 模块开发文档

## 1. 模块概述

### 1.1 简介

RBAC（Role-Based Access Control，基于角色的访问控制）模块是 Fake 框架的核心权限管理模块，提供了完整的用户、角色、权限、菜单管理能力。

### 1.2 设计目标

- **灵活性**：支持多租户场景下的权限隔离
- **可扩展性**：遵循 DDD 设计原则，便于扩展业务逻辑
- **高性能**：支持权限缓存，减少数据库查询
- **易用性**：提供清晰的 API 接口和完善的文档

### 1.3 功能清单

- [x] 用户管理（基础 CRUD）
- [x] 角色管理（基础实体）
- [x] 用户-角色关联
- [x] 角色-权限关联
- [x] 菜单管理（基础实体）
- [ ] 完整的应用服务层
- [ ] 权限验证拦截器
- [ ] 动态权限加载
- [ ] 权限缓存策略
- [ ] 数据权限过滤
- [ ] 权限变更审计

## 2. 架构设计

### 2.1 模块分层

```
┌─────────────────────────────────────────┐
│     Fake.Rbac.HttpApi (未实现)          │  HTTP API 层
├─────────────────────────────────────────┤
│     Fake.Rbac.Application               │  应用服务层
├─────────────────────────────────────────┤
│     Fake.Rbac.Domain                    │  领域层
├─────────────────────────────────────────┤
│     Fake.Rbac.Infrastructure            │  基础设施层
└─────────────────────────────────────────┘
```

### 2.2 模块依赖关系

```
FakeRbacApplicationModule
    └─> FakeRbacDomainModule
            └─> FakeDomainDrivenDesignModule
                    └─> FakeCoreModule

FakeRbacInfrastructureModule
    └─> FakeRbacDomainModule
    └─> FakeEntityFrameworkCoreModule
```

### 2.3 核心概念

#### 2.3.1 RBAC 模型

```
User（用户）<─────多对多─────> Role（角色）<─────多对多─────> Permission（权限）
                 UserRole                      RolePermission
                 
Menu（菜单）<─────关联─────> Permission（权限）
```

- **User（用户）**：系统使用者，可以拥有多个角色
- **Role（角色）**：权限的集合，一个角色可以关联多个权限
- **Permission（权限）**：最小的权限单元，通常对应一个操作或资源
- **Menu（菜单）**：前端菜单和按钮，可以关联权限进行访问控制

## 3. 领域模型设计

### 3.1 聚合根

#### 3.1.1 User（用户聚合根）

**职责**：
- 管理用户基本信息
- 管理用户密码（加密存储）
- 管理用户角色关联
- 提供角色分配/撤销操作

**关键属性**：
- `Name`：用户名称
- `Account`：账号（登录凭证）
- `EncryptPassword`：加密后的密码（值对象）
- `Email`：邮箱
- `Avatar`：头像
- `Roles`：角色集合（导航属性）

**关键方法**：
```csharp
void AssignRole(Guid roleId)           // 分配角色
void RemoveRole(Guid roleId)           // 移除角色
void ClearRoles()                       // 清空角色
bool HasRole(Guid roleId)               // 是否拥有角色
void GeneratePassword(string password)  // 生成密码
```

#### 3.1.2 Role（角色聚合根）

**职责**：
- 管理角色基本信息
- 管理角色权限关联
- 提供权限分配/撤销操作

**关键属性**：
- `Name`：角色名称
- `Code`：角色编码（唯一标识）
- `Permissions`：权限集合（导航属性）

**关键方法**：
```csharp
void AddPermission(string permissionCode)    // 添加权限
void RemovePermission(string permissionCode) // 移除权限
```

#### 3.1.3 Menu（菜单聚合根）

**职责**：
- 管理菜单基本信息
- 管理菜单层级关系
- 关联权限代码

**关键属性**：
- `PId`：父级菜单 ID
- `Name`：菜单名称
- `PermissionCode`：关联的权限代码
- `Type`：菜单类型（菜单/按钮）
- `Icon`：图标
- `Route`：路由地址
- `Component`：前端组件路径
- `Order`：排序
- `IsHidden`：是否隐藏
- `IsCached`：是否缓存
- `Children`：子菜单集合

**关键方法**：
```csharp
void AddChild(Menu menu)    // 添加子菜单
void RemoveChild(Menu menu) // 移除子菜单
```

### 3.2 实体

#### 3.2.1 UserRole（用户角色关联）

多对多关联表，记录用户与角色的关系。

```csharp
public class UserRole : CreateAuditedEntity<Guid>
{
    public Guid UserId { get; private set; }
    public Guid RoleId { get; private set; }
}
```

#### 3.2.2 RolePermission（角色权限关联）

多对多关联表，记录角色与权限的关系。

```csharp
public class RolePermission : CreateAuditedEntity<Guid>
{
    public Guid RoleId { get; private set; }
    public string PermissionCode { get; private set; }
}
```

### 3.3 值对象

#### 3.3.1 EncryptPassword（加密密码）

封装密码和盐值，确保密码安全存储。

```csharp
public class EncryptPassword
{
    public string Password { get; private set; }
    public string Salt { get; private set; }
}
```

### 3.4 枚举

#### 3.4.1 MenuType（菜单类型）

```csharp
public enum MenuType
{
    Menu = 1,      // 菜单
    Button = 2     // 按钮
}
```

### 3.5 仓储接口

```csharp
public interface IUserRepository : IRepository<User, Guid>
{
    Task<User?> FindByAccountAsync(string account);
    Task<List<User>> GetUsersWithRolesAsync(List<Guid> userIds);
}

public interface IRoleRepository : IRepository<Role, Guid>
{
    Task<Role?> FindByCodeAsync(string code);
    Task<List<Role>> GetRolesWithPermissionsAsync(List<Guid> roleIds);
}

public interface IMenuRepository : IRepository<Menu, Guid>
{
    Task<List<Menu>> GetMenuTreeAsync(Guid? parentId = null);
    Task<List<Menu>> GetMenusByPermissionsAsync(List<string> permissionCodes);
}
```

### 3.6 领域服务

#### 3.6.1 AccountManager（账号管理器）

```csharp
public class AccountManager : IDomainService
{
    // 校验账号唯一性
    Task ValidateAccountUniqueAsync(string account);
    
    // 校验密码强度
    bool ValidatePasswordStrength(string password);
    
    // 验证用户凭证
    Task<User?> ValidateCredentialsAsync(string account, string password);
}
```

## 4. 应用服务层设计

### 4.1 应用服务

#### 4.1.1 UserService（用户服务）

**职责**：用户管理相关的应用逻辑

**接口设计**：

```csharp
public interface IUserService : IApplicationService
{
    // 查询
    Task<UserDto> GetAsync(Guid id);
    Task<PagedResultDto<UserDto>> GetListAsync(UserPagedRequestDto input);
    Task<List<UserSimpleDto>> GetUsersByRoleAsync(Guid roleId);
    
    // 创建
    Task<UserDto> CreateAsync(UserCreateDto input);
    
    // 更新
    Task<UserDto> UpdateAsync(Guid id, UserUpdateDto input);
    Task UpdatePasswordAsync(Guid id, UpdatePasswordDto input);
    Task UpdateAvatarAsync(Guid id, string avatarUrl);
    
    // 删除
    Task DeleteAsync(Guid id);
    Task DeleteBatchAsync(List<Guid> ids);
    
    // 角色管理
    Task AssignRolesAsync(Guid userId, List<Guid> roleIds);
    Task<List<RoleDto>> GetUserRolesAsync(Guid userId);
    
    // 权限查询
    Task<List<string>> GetUserPermissionsAsync(Guid userId);
    Task<bool> HasPermissionAsync(Guid userId, string permissionCode);
}
```

#### 4.1.2 RoleService（角色服务）

**职责**：角色管理相关的应用逻辑

**接口设计**：

```csharp
public interface IRoleService : IApplicationService
{
    // 查询
    Task<RoleDto> GetAsync(Guid id);
    Task<PagedResultDto<RoleDto>> GetListAsync(RolePagedRequestDto input);
    Task<List<RoleSimpleDto>> GetAllRolesAsync();
    
    // 创建
    Task<RoleDto> CreateAsync(RoleCreateDto input);
    
    // 更新
    Task<RoleDto> UpdateAsync(Guid id, RoleUpdateDto input);
    
    // 删除
    Task DeleteAsync(Guid id);
    
    // 权限管理
    Task AssignPermissionsAsync(Guid roleId, List<string> permissionCodes);
    Task<List<string>> GetRolePermissionsAsync(Guid roleId);
    
    // 用户管理
    Task<List<UserSimpleDto>> GetRoleUsersAsync(Guid roleId);
    Task<int> GetRoleUserCountAsync(Guid roleId);
}
```

#### 4.1.3 MenuService（菜单服务）

**职责**：菜单管理相关的应用逻辑

**接口设计**：

```csharp
public interface IMenuService : IApplicationService
{
    // 查询
    Task<MenuDto> GetAsync(Guid id);
    Task<List<MenuTreeDto>> GetMenuTreeAsync();
    Task<List<MenuDto>> GetUserMenusAsync(Guid userId);
    
    // 创建
    Task<MenuDto> CreateAsync(MenuCreateDto input);
    
    // 更新
    Task<MenuDto> UpdateAsync(Guid id, MenuUpdateDto input);
    Task UpdateOrderAsync(Guid id, int order);
    
    // 删除
    Task DeleteAsync(Guid id);
    
    // 菜单树操作
    Task MoveMenuAsync(Guid menuId, Guid? targetParentId);
}
```

#### 4.1.4 PermissionService（权限服务）

**职责**：权限定义和查询

**接口设计**：

```csharp
public interface IPermissionService : IApplicationService
{
    // 获取所有权限定义
    Task<List<PermissionDefinitionDto>> GetAllPermissionsAsync();
    
    // 获取权限树（按模块分组）
    Task<List<PermissionGroupDto>> GetPermissionTreeAsync();
    
    // 校验权限
    Task<bool> CheckPermissionAsync(Guid userId, string permissionCode);
    Task<Dictionary<string, bool>> CheckPermissionsAsync(Guid userId, List<string> permissionCodes);
}
```

### 4.2 DTO 设计

#### 4.2.1 User DTOs

```csharp
// 详情 DTO
public class UserDto : AuditedEntityDto<Guid>
{
    public string Name { get; set; }
    public string Account { get; set; }
    public string? Email { get; set; }
    public string? Avatar { get; set; }
    public List<RoleSimpleDto> Roles { get; set; }
}

// 简单 DTO
public class UserSimpleDto : EntityDto<Guid>
{
    public string Name { get; set; }
    public string Account { get; set; }
    public string? Avatar { get; set; }
}

// 创建 DTO
public class UserCreateDto
{
    [Required, StringLength(50)]
    public string Name { get; set; }
    
    [Required, StringLength(50)]
    public string Account { get; set; }
    
    [Required, StringLength(100, MinimumLength = 6)]
    public string Password { get; set; }
    
    [EmailAddress]
    public string? Email { get; set; }
    
    public List<Guid>? RoleIds { get; set; }
}

// 更新 DTO
public class UserUpdateDto
{
    [StringLength(50)]
    public string? Name { get; set; }
    
    [EmailAddress]
    public string? Email { get; set; }
}

// 修改密码 DTO
public class UpdatePasswordDto
{
    [Required]
    public string OldPassword { get; set; }
    
    [Required, StringLength(100, MinimumLength = 6)]
    public string NewPassword { get; set; }
}

// 分页查询 DTO
public class UserPagedRequestDto : PagedRequestDto
{
    public string? Keyword { get; set; }
    public Guid? RoleId { get; set; }
}
```

#### 4.2.2 Role DTOs

```csharp
public class RoleDto : AuditedEntityDto<Guid>
{
    public string Name { get; set; }
    public string Code { get; set; }
    public List<string> Permissions { get; set; }
}

public class RoleSimpleDto : EntityDto<Guid>
{
    public string Name { get; set; }
    public string Code { get; set; }
}

public class RoleCreateDto
{
    [Required, StringLength(50)]
    public string Name { get; set; }
    
    [Required, StringLength(50)]
    public string Code { get; set; }
    
    public List<string>? Permissions { get; set; }
}

public class RoleUpdateDto
{
    [StringLength(50)]
    public string? Name { get; set; }
}

public class RolePagedRequestDto : PagedRequestDto
{
    public string? Keyword { get; set; }
}
```

#### 4.2.3 Menu DTOs

```csharp
public class MenuDto : AuditedEntityDto<Guid>
{
    public Guid PId { get; set; }
    public string Name { get; set; }
    public string? PermissionCode { get; set; }
    public MenuType Type { get; set; }
    public string? Icon { get; set; }
    public string? Route { get; set; }
    public string? Component { get; set; }
    public int Order { get; set; }
    public bool IsHidden { get; set; }
    public bool IsCached { get; set; }
    public string? Description { get; set; }
}

public class MenuTreeDto : MenuDto
{
    public List<MenuTreeDto> Children { get; set; }
}

public class MenuCreateDto
{
    public Guid? PId { get; set; }
    
    [Required, StringLength(50)]
    public string Name { get; set; }
    
    [StringLength(100)]
    public string? PermissionCode { get; set; }
    
    [Required]
    public MenuType Type { get; set; }
    
    public string? Icon { get; set; }
    public string? Route { get; set; }
    public string? Component { get; set; }
    public int Order { get; set; }
    public bool IsHidden { get; set; }
    public bool IsCached { get; set; }
    public string? Description { get; set; }
}

public class MenuUpdateDto
{
    [StringLength(50)]
    public string? Name { get; set; }
    
    [StringLength(100)]
    public string? PermissionCode { get; set; }
    
    public string? Icon { get; set; }
    public string? Route { get; set; }
    public string? Component { get; set; }
    public bool? IsHidden { get; set; }
    public bool? IsCached { get; set; }
    public string? Description { get; set; }
}
```

## 5. 基础设施层设计

### 5.1 数据库设计

#### 5.1.1 表结构

**Users 表**：
```sql
CREATE TABLE Users (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    Name NVARCHAR(50) NOT NULL,
    Account NVARCHAR(50) NOT NULL UNIQUE,
    Password NVARCHAR(100) NOT NULL,
    Salt NVARCHAR(50) NOT NULL,
    Email NVARCHAR(100),
    Avatar NVARCHAR(500),
    CreatedAt DATETIME2 NOT NULL,
    CreatedBy UNIQUEIDENTIFIER,
    UpdatedAt DATETIME2,
    UpdatedBy UNIQUEIDENTIFIER,
    DeletedAt DATETIME2,
    DeletedBy UNIQUEIDENTIFIER,
    IsDeleted BIT NOT NULL DEFAULT 0
);

CREATE INDEX IX_Users_Account ON Users(Account) WHERE IsDeleted = 0;
```

**Roles 表**：
```sql
CREATE TABLE Roles (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    Name NVARCHAR(50) NOT NULL,
    Code NVARCHAR(50) NOT NULL UNIQUE,
    CreatedAt DATETIME2 NOT NULL,
    CreatedBy UNIQUEIDENTIFIER,
    UpdatedAt DATETIME2,
    UpdatedBy UNIQUEIDENTIFIER,
    DeletedAt DATETIME2,
    DeletedBy UNIQUEIDENTIFIER,
    IsDeleted BIT NOT NULL DEFAULT 0
);

CREATE INDEX IX_Roles_Code ON Roles(Code) WHERE IsDeleted = 0;
```

**UserRoles 表**：
```sql
CREATE TABLE UserRoles (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    UserId UNIQUEIDENTIFIER NOT NULL,
    RoleId UNIQUEIDENTIFIER NOT NULL,
    CreatedAt DATETIME2 NOT NULL,
    CreatedBy UNIQUEIDENTIFIER,
    FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE,
    FOREIGN KEY (RoleId) REFERENCES Roles(Id) ON DELETE CASCADE
);

CREATE UNIQUE INDEX IX_UserRoles_UserId_RoleId ON UserRoles(UserId, RoleId);
```

**RolePermissions 表**：
```sql
CREATE TABLE RolePermissions (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    RoleId UNIQUEIDENTIFIER NOT NULL,
    PermissionCode NVARCHAR(100) NOT NULL,
    CreatedAt DATETIME2 NOT NULL,
    CreatedBy UNIQUEIDENTIFIER,
    FOREIGN KEY (RoleId) REFERENCES Roles(Id) ON DELETE CASCADE
);

CREATE UNIQUE INDEX IX_RolePermissions_RoleId_PermissionCode 
    ON RolePermissions(RoleId, PermissionCode);
```

**Menus 表**：
```sql
CREATE TABLE Menus (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    PId UNIQUEIDENTIFIER,
    Name NVARCHAR(50) NOT NULL,
    PermissionCode NVARCHAR(100),
    Type INT NOT NULL,
    Icon NVARCHAR(100),
    Route NVARCHAR(200),
    Component NVARCHAR(200),
    [Order] INT NOT NULL DEFAULT 0,
    IsHidden BIT NOT NULL DEFAULT 0,
    IsCached BIT NOT NULL DEFAULT 0,
    Description NVARCHAR(500),
    CreatedAt DATETIME2 NOT NULL,
    CreatedBy UNIQUEIDENTIFIER,
    UpdatedAt DATETIME2,
    UpdatedBy UNIQUEIDENTIFIER,
    DeletedAt DATETIME2,
    DeletedBy UNIQUEIDENTIFIER,
    IsDeleted BIT NOT NULL DEFAULT 0,
    FOREIGN KEY (PId) REFERENCES Menus(Id)
);

CREATE INDEX IX_Menus_PId ON Menus(PId) WHERE IsDeleted = 0;
CREATE INDEX IX_Menus_PermissionCode ON Menus(PermissionCode) WHERE IsDeleted = 0;
```

### 5.2 EF Core 配置

#### 5.2.1 UserEntityTypeConfiguration

```csharp
public class UserEntityTypeConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users", RbacDbContext.DefaultSchema);
        
        builder.Property(x => x.Name).IsRequired().HasMaxLength(50);
        builder.Property(x => x.Account).IsRequired().HasMaxLength(50);
        
        builder.OwnsOne(x => x.EncryptPassword, password =>
        {
            password.Property(p => p.Password).HasColumnName("Password")
                .IsRequired().HasMaxLength(100);
            password.Property(p => p.Salt).HasColumnName("Salt")
                .IsRequired().HasMaxLength(50);
        });
        
        builder.Property(x => x.Email).HasMaxLength(100);
        builder.Property(x => x.Avatar).HasMaxLength(500);
        
        builder.HasIndex(x => x.Account).IsUnique()
            .HasFilter("[IsDeleted] = 0");
        
        builder.HasMany(x => x.Roles)
            .WithOne()
            .HasForeignKey(ur => ur.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
```

### 5.3 仓储实现

```csharp
public class UserRepository : EfCoreRepository<RbacDbContext, User, Guid>, IUserRepository
{
    public async Task<User?> FindByAccountAsync(string account)
    {
        return await DbSet
            .Include(u => u.Roles)
            .FirstOrDefaultAsync(u => u.Account == account);
    }

    public async Task<List<User>> GetUsersWithRolesAsync(List<Guid> userIds)
    {
        return await DbSet
            .Include(u => u.Roles)
            .Where(u => userIds.Contains(u.Id))
            .ToListAsync();
    }
}
```

## 6. HTTP API 设计

### 6.1 RESTful API 规范

**基础路径**：`/api/rbac`

### 6.2 User API

```
# 用户管理
GET    /api/rbac/users              # 获取用户列表（分页）
GET    /api/rbac/users/{id}         # 获取用户详情
POST   /api/rbac/users              # 创建用户
PUT    /api/rbac/users/{id}         # 更新用户
DELETE /api/rbac/users/{id}         # 删除用户
DELETE /api/rbac/users/batch        # 批量删除用户

# 用户密码
PUT    /api/rbac/users/{id}/password    # 修改密码
PUT    /api/rbac/users/{id}/avatar      # 更新头像

# 用户角色
GET    /api/rbac/users/{id}/roles       # 获取用户角色
PUT    /api/rbac/users/{id}/roles       # 分配用户角色

# 用户权限
GET    /api/rbac/users/{id}/permissions # 获取用户权限
```

### 6.3 Role API

```
# 角色管理
GET    /api/rbac/roles              # 获取角色列表
GET    /api/rbac/roles/all          # 获取所有角色（不分页）
GET    /api/rbac/roles/{id}         # 获取角色详情
POST   /api/rbac/roles              # 创建角色
PUT    /api/rbac/roles/{id}         # 更新角色
DELETE /api/rbac/roles/{id}         # 删除角色

# 角色权限
GET    /api/rbac/roles/{id}/permissions # 获取角色权限
PUT    /api/rbac/roles/{id}/permissions # 分配角色权限

# 角色用户
GET    /api/rbac/roles/{id}/users       # 获取角色用户列表
GET    /api/rbac/roles/{id}/users/count # 获取角色用户数量
```

### 6.4 Menu API

```
# 菜单管理
GET    /api/rbac/menus              # 获取菜单树
GET    /api/rbac/menus/{id}         # 获取菜单详情
POST   /api/rbac/menus              # 创建菜单
PUT    /api/rbac/menus/{id}         # 更新菜单
DELETE /api/rbac/menus/{id}         # 删除菜单

# 菜单操作
PUT    /api/rbac/menus/{id}/move    # 移动菜单
PUT    /api/rbac/menus/{id}/order   # 更新排序

# 用户菜单
GET    /api/rbac/menus/user         # 获取当前用户菜单
```

### 6.5 Permission API

```
# 权限管理
GET    /api/rbac/permissions        # 获取所有权限定义
GET    /api/rbac/permissions/tree   # 获取权限树
POST   /api/rbac/permissions/check  # 批量检查权限
```

## 7. 权限定义

### 7.1 权限提供器

```csharp
public class RbacPermissionDefinitionProvider : IPermissionDefinitionProvider
{
    public void Define(IPermissionDefinitionContext context)
    {
        var rbacGroup = context.AddGroup("Rbac", "权限管理");

        // 用户管理
        var userPermission = rbacGroup.AddPermission("Rbac.Users", "用户管理");
        userPermission.AddChild("Rbac.Users.Query", "查询用户");
        userPermission.AddChild("Rbac.Users.Create", "创建用户");
        userPermission.AddChild("Rbac.Users.Update", "更新用户");
        userPermission.AddChild("Rbac.Users.Delete", "删除用户");

        // 角色管理
        var rolePermission = rbacGroup.AddPermission("Rbac.Roles", "角色管理");
        rolePermission.AddChild("Rbac.Roles.Query", "查询角色");
        rolePermission.AddChild("Rbac.Roles.Create", "创建角色");
        rolePermission.AddChild("Rbac.Roles.Update", "更新角色");
        rolePermission.AddChild("Rbac.Roles.Delete", "删除角色");
        rolePermission.AddChild("Rbac.Roles.AssignPermissions", "分配权限");

        // 菜单管理
        var menuPermission = rbacGroup.AddPermission("Rbac.Menus", "菜单管理");
        menuPermission.AddChild("Rbac.Menus.Query", "查询菜单");
        menuPermission.AddChild("Rbac.Menus.Create", "创建菜单");
        menuPermission.AddChild("Rbac.Menus.Update", "更新菜单");
        menuPermission.AddChild("Rbac.Menus.Delete", "删除菜单");
    }
}
```

### 7.2 权限验证

```csharp
// 使用特性标记
[Authorize(Permission = "Rbac.Users.Create")]
public async Task<UserDto> CreateAsync(UserCreateDto input)
{
    // ...
}

// 手动检查
if (!await _permissionChecker.IsGrantedAsync("Rbac.Users.Delete"))
{
    throw new FakeAuthorizationException("没有删除权限");
}
```

## 8. 缓存策略

### 8.1 缓存设计

```csharp
// 用户权限缓存
CacheKey: "rbac:user:{userId}:permissions"
Value: List<string> (权限代码列表)
Expiration: 30 minutes

// 角色权限缓存
CacheKey: "rbac:role:{roleId}:permissions"
Value: List<string> (权限代码列表)
Expiration: 1 hour

// 用户菜单缓存
CacheKey: "rbac:user:{userId}:menus"
Value: List<MenuTreeDto>
Expiration: 30 minutes
```

### 8.2 缓存失效策略

- 用户分配/移除角色时，清除该用户的权限和菜单缓存
- 角色分配/移除权限时，清除该角色的权限缓存，同时清除拥有该角色的所有用户的权限和菜单缓存
- 菜单修改时，清除所有用户的菜单缓存

## 9. 开发规范

### 9.1 命名规范

- **实体**：使用名词，首字母大写，如 `User`、`Role`
- **值对象**：使用名词，如 `EncryptPassword`
- **仓储**：`I{Entity}Repository`，如 `IUserRepository`
- **应用服务**：`{Entity}Service`，如 `UserService`
- **DTO**：`{Entity}{Action}Dto`，如 `UserCreateDto`
- **领域服务**：`{业务}Manager`，如 `AccountManager`

### 9.2 代码规范

- 遵循 DDD 原则，聚合根封装业务逻辑
- 使用私有 setter，通过方法修改状态
- 仓储只返回聚合根，不返回关联实体
- 应用服务处理事务边界
- DTO 只用于数据传输，不包含业务逻辑

### 9.3 异常处理

```csharp
// 领域异常
public class UserNotFoundException : FakeDomainException
{
    public UserNotFoundException(Guid userId) 
        : base($"用户不存在：{userId}")
    {
    }
}

// 业务异常
public class DuplicateAccountException : FakeBusinessException
{
    public DuplicateAccountException(string account)
        : base($"账号已存在：{account}")
    {
    }
}
```

## 10. 开发计划

### 10.1 阶段一：完善领域层（已完成）

- [x] User 聚合根及相关实体
- [x] Role 聚合根及相关实体
- [x] Menu 聚合根
- [x] 仓储接口定义
- [x] 领域服务（AccountManager）

### 10.2 阶段二：实现基础设施层

- [ ] 完善 EF Core 配置
- [ ] 实现仓储
- [ ] 数据库迁移
- [ ] 种子数据

### 10.3 阶段三：实现应用服务层

- [ ] UserService 完整实现
- [ ] RoleService 实现
- [ ] MenuService 实现
- [ ] PermissionService 实现
- [ ] AutoMapper 配置
- [ ] DTO 验证

### 10.4 阶段四：实现 HTTP API 层

- [ ] UserController
- [ ] RoleController
- [ ] MenuController
- [ ] PermissionController
- [ ] API 文档（Swagger）

### 10.5 阶段五：权限验证

- [ ] 权限定义提供器
- [ ] 权限检查器
- [ ] 权限特性拦截器
- [ ] 动态权限策略

### 10.6 阶段六：缓存优化

- [ ] Redis 缓存集成
- [ ] 权限缓存实现
- [ ] 菜单缓存实现
- [ ] 缓存失效策略

### 10.7 阶段七：高级功能

- [ ] 数据权限过滤
- [ ] 权限变更审计
- [ ] 权限导入导出
- [ ] 多租户支持

### 10.8 阶段八：测试

- [ ] 单元测试（Domain）
- [ ] 集成测试（Application）
- [ ] API 测试（HttpApi）
- [ ] 性能测试

## 11. 测试用例

### 11.1 用户管理测试

```csharp
[Fact]
public async Task Should_Create_User_With_Valid_Data()
{
    // Arrange
    var input = new UserCreateDto
    {
        Name = "测试用户",
        Account = "test001",
        Password = "123456",
        Email = "test@example.com"
    };

    // Act
    var result = await _userService.CreateAsync(input);

    // Assert
    Assert.NotNull(result);
    Assert.Equal(input.Name, result.Name);
}

[Fact]
public async Task Should_Throw_Exception_When_Create_User_With_Duplicate_Account()
{
    // Arrange
    var input = new UserCreateDto
    {
        Name = "测试用户",
        Account = "admin", // 已存在的账号
        Password = "123456"
    };

    // Act & Assert
    await Assert.ThrowsAsync<DuplicateAccountException>(
        () => _userService.CreateAsync(input)
    );
}
```

### 11.2 角色权限测试

```csharp
[Fact]
public async Task Should_Assign_Permissions_To_Role()
{
    // Arrange
    var roleId = Guid.NewGuid();
    var permissions = new List<string> 
    { 
        "Rbac.Users.Query", 
        "Rbac.Users.Create" 
    };

    // Act
    await _roleService.AssignPermissionsAsync(roleId, permissions);
    var result = await _roleService.GetRolePermissionsAsync(roleId);

    // Assert
    Assert.Equal(permissions.Count, result.Count);
    Assert.Contains("Rbac.Users.Query", result);
}
```

## 12. 参考资料

### 12.1 设计模式

- **DDD（领域驱动设计）**：聚合根、实体、值对象、仓储
- **CQRS**：命令查询职责分离
- **Repository Pattern**：仓储模式

### 12.2 技术栈

- **.NET 8**
- **Entity Framework Core**
- **AutoMapper**
- **Redis**（缓存）
- **Autofac**（DI容器）

### 12.3 相关文档

- [Fake 核心模块文档](../../docs/design/核心模块.md)
- [租户管理模块](../tenant-management/README.md)
- [ABP 权限系统](https://docs.abp.io/en/abp/latest/Authorization)

## 13. 常见问题

### Q1: 如何扩展权限验证逻辑？

实现自定义的 `IPermissionChecker`，并在模块中替换默认实现。

### Q2: 如何支持多租户？

User、Role、Menu 实体已继承 `FullAuditedAggregateRoot`，默认支持软删除。
如需多租户支持，可让实体实现 `IMultiTenant` 接口，并启用多租户模块。

### Q3: 如何实现数据权限？

可以通过实现自定义的查询过滤器，在仓储层面根据用户权限过滤数据。

## 14. 更新日志

### v1.0.0 (开发中)

- 初始版本
- 完成领域模型设计
- 完成基础的用户、角色、菜单实体

---

**文档维护人员**：开发团队  
**最后更新时间**：2025-11-05  
**文档版本**：v1.0.0

