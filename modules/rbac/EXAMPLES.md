# RBAC模块使用示例

## 1. 用户管理示例

### 1.1 创建用户

```csharp
// 创建普通用户
var createDto = new UserCreateDto
{
    Name = "张三",
    Account = "zhangsan",
    Password = "Pass@123",
    Email = "zhangsan@example.com",
    Avatar = "https://example.com/avatar.jpg"
};

var user = await _userService.CreateAsync(createDto);
Console.WriteLine($"用户创建成功，ID: {user.Id}");
```

### 1.2 创建用户并分配角色

```csharp
var createDto = new UserCreateDto
{
    Name = "李四",
    Account = "lisi",
    Password = "Pass@123",
    Email = "lisi@example.com",
    RoleIds = new List<Guid> { adminRoleId, editorRoleId }
};

var user = await _userService.CreateAsync(createDto);
```

### 1.3 查询用户列表

```csharp
// 分页查询
var request = new UserPagedRequestDto
{
    Page = 1,
    PageSize = 20,
    Keyword = "张",  // 搜索关键字
    RoleId = adminRoleId,  // 按角色筛选
    Descending = true  // 降序排列
};

var result = await _userService.GetListAsync(request);
Console.WriteLine($"总数: {result.Total}");
foreach (var user in result.Items)
{
    Console.WriteLine($"用户: {user.Name} ({user.Account})");
}
```

### 1.4 更新用户信息

```csharp
var updateDto = new UserUpdateDto
{
    Name = "张三（已更新）",
    Email = "zhangsan_new@example.com"
};

var user = await _userService.UpdateAsync(userId, updateDto);
```

### 1.5 修改密码

```csharp
var passwordDto = new UpdatePasswordDto
{
    OldPassword = "Pass@123",
    NewPassword = "NewPass@456"
};

await _userService.UpdatePasswordAsync(userId, passwordDto);
```

### 1.6 分配角色

```csharp
// 为用户分配多个角色
var roleIds = new List<Guid> { role1Id, role2Id, role3Id };
await _userService.AssignRolesAsync(userId, roleIds);
```

### 1.7 获取用户角色

```csharp
var roles = await _userService.GetUserRolesAsync(userId);
foreach (var role in roles)
{
    Console.WriteLine($"角色: {role.Name} ({role.Code})");
}
```

### 1.8 获取用户权限

```csharp
var permissions = await _userService.GetUserPermissionsAsync(userId);
foreach (var permission in permissions)
{
    Console.WriteLine($"权限: {permission}");
}
```

### 1.9 检查用户权限

```csharp
bool canCreate = await _userService.HasPermissionAsync(userId, "Rbac.Users.Create");
if (canCreate)
{
    Console.WriteLine("用户有创建权限");
}
else
{
    Console.WriteLine("用户没有创建权限");
}
```

### 1.10 批量删除用户

```csharp
var userIds = new List<Guid> { user1Id, user2Id, user3Id };
await _userService.DeleteBatchAsync(userIds);
```

## 2. 角色管理示例

### 2.1 创建角色

```csharp
var createDto = new RoleCreateDto
{
    Name = "编辑员",
    Code = "Editor",
    Permissions = new List<string>
    {
        "Rbac.Users.Query",
        "Rbac.Users.Update"
    }
};

var role = await _roleService.CreateAsync(createDto);
```

### 2.2 查询角色列表

```csharp
// 分页查询
var request = new RolePagedRequestDto
{
    Page = 1,
    PageSize = 20,
    Keyword = "管理"
};

var result = await _roleService.GetListAsync(request);
```

### 2.3 获取所有角色（不分页）

```csharp
var allRoles = await _roleService.GetAllRolesAsync();
foreach (var role in allRoles)
{
    Console.WriteLine($"{role.Name} - {role.Code}");
}
```

### 2.4 更新角色

```csharp
var updateDto = new RoleUpdateDto
{
    Name = "高级编辑员"
};

var role = await _roleService.UpdateAsync(roleId, updateDto);
```

### 2.5 分配权限

```csharp
var permissions = new List<string>
{
    "Rbac.Users",
    "Rbac.Users.Query",
    "Rbac.Users.Create",
    "Rbac.Users.Update",
    "Rbac.Roles.Query"
};

await _roleService.AssignPermissionsAsync(roleId, permissions);
```

### 2.6 获取角色权限

```csharp
var permissions = await _roleService.GetRolePermissionsAsync(roleId);
Console.WriteLine($"角色拥有 {permissions.Count} 个权限");
```

### 2.7 获取角色用户

```csharp
var users = await _roleService.GetRoleUsersAsync(roleId);
foreach (var user in users)
{
    Console.WriteLine($"用户: {user.Name} ({user.Account})");
}
```

### 2.8 获取角色用户数量

```csharp
int userCount = await _roleService.GetRoleUserCountAsync(roleId);
Console.WriteLine($"该角色有 {userCount} 个用户");
```

### 2.9 删除角色

```csharp
try
{
    await _roleService.DeleteAsync(roleId);
    Console.WriteLine("角色删除成功");
}
catch (DomainException ex)
{
    Console.WriteLine($"删除失败: {ex.Message}");
    // 可能是因为角色下还有用户
}
```

## 3. 菜单管理示例

### 3.1 创建顶级菜单

```csharp
var createDto = new MenuCreateDto
{
    PId = null,  // 顶级菜单
    Name = "系统管理",
    Type = MenuType.Menu,
    PermissionCode = "System",
    Icon = "setting",
    Route = "/system",
    Order = 100,
    IsHidden = false,
    IsCached = false,
    Description = "系统管理模块"
};

var menu = await _menuService.CreateAsync(createDto);
```

### 3.2 创建子菜单

```csharp
var createDto = new MenuCreateDto
{
    PId = parentMenuId,  // 父级菜单ID
    Name = "用户管理",
    Type = MenuType.Menu,
    PermissionCode = "Rbac.Users",
    Icon = "user",
    Route = "/system/users",
    Component = "system/users/index",
    Order = 1,
    IsHidden = false,
    IsCached = true,
    Description = "用户管理页面"
};

var menu = await _menuService.CreateAsync(createDto);
```

### 3.3 创建按钮

```csharp
var createDto = new MenuCreateDto
{
    PId = userMenuId,  // 用户管理菜单ID
    Name = "新增",
    Type = MenuType.Button,
    PermissionCode = "Rbac.Users.Create",
    Order = 1
};

var button = await _menuService.CreateAsync(createDto);
```

### 3.4 获取菜单树

```csharp
var menuTree = await _menuService.GetMenuTreeAsync();

void PrintMenu(List<MenuTreeDto> menus, int level = 0)
{
    foreach (var menu in menus)
    {
        var indent = new string(' ', level * 2);
        Console.WriteLine($"{indent}{menu.Name} ({menu.Type})");
        
        if (menu.Children.Any())
        {
            PrintMenu(menu.Children, level + 1);
        }
    }
}

PrintMenu(menuTree);
```

### 3.5 获取用户菜单

```csharp
var userMenus = await _menuService.GetUserMenusAsync(userId);
// 返回的是根据用户权限过滤后的菜单树
```

### 3.6 更新菜单

```csharp
var updateDto = new MenuUpdateDto
{
    Name = "用户管理（已更新）",
    Icon = "user-group",
    IsHidden = false
};

var menu = await _menuService.UpdateAsync(menuId, updateDto);
```

### 3.7 更新菜单排序

```csharp
await _menuService.UpdateOrderAsync(menuId, 10);
```

### 3.8 移动菜单

```csharp
// 移动到新的父级菜单下
await _menuService.MoveMenuAsync(menuId, newParentId);

// 移动到顶级
await _menuService.MoveMenuAsync(menuId, null);
```

### 3.9 删除菜单

```csharp
try
{
    await _menuService.DeleteAsync(menuId);
    Console.WriteLine("菜单删除成功");
}
catch (DomainException ex)
{
    Console.WriteLine($"删除失败: {ex.Message}");
    // 可能是因为菜单下还有子菜单
}
```

## 4. 权限管理示例

### 4.1 获取所有权限定义

```csharp
var permissions = await _permissionService.GetAllPermissionsAsync();
foreach (var permission in permissions)
{
    Console.WriteLine($"{permission.Code} - {permission.Name}");
    if (!string.IsNullOrEmpty(permission.Description))
    {
        Console.WriteLine($"  描述: {permission.Description}");
    }
}
```

### 4.2 获取权限树

```csharp
var permissionTree = await _permissionService.GetPermissionTreeAsync();
foreach (var group in permissionTree)
{
    Console.WriteLine($"模块: {group.Name}");
    foreach (var permission in group.Permissions)
    {
        Console.WriteLine($"  - {permission.Code}: {permission.Name}");
    }
}
```

### 4.3 检查单个权限

```csharp
bool hasPermission = await _permissionService.CheckPermissionAsync(
    userId, 
    "Rbac.Users.Create"
);

if (hasPermission)
{
    Console.WriteLine("用户有创建权限");
}
```

### 4.4 批量检查权限

```csharp
var permissionsToCheck = new List<string>
{
    "Rbac.Users.Create",
    "Rbac.Users.Update",
    "Rbac.Users.Delete"
};

var result = await _permissionService.CheckPermissionsAsync(userId, permissionsToCheck);

foreach (var kvp in result)
{
    Console.WriteLine($"{kvp.Key}: {(kvp.Value ? "有权限" : "无权限")}");
}
```

## 5. 认证服务示例

### 5.1 用户登录

```csharp
try
{
    var userInfo = await _authService.LoginAsync("admin", "123456");
    
    Console.WriteLine($"登录成功: {userInfo.Name}");
    Console.WriteLine($"角色: {string.Join(", ", userInfo.Roles.Select(r => r.Name))}");
    Console.WriteLine($"权限数量: {userInfo.Permissions.Count}");
    Console.WriteLine($"菜单数量: {userInfo.Menus.Count}");
}
catch (DomainException ex)
{
    Console.WriteLine($"登录失败: {ex.Message}");
}
```

### 5.2 获取当前用户信息

```csharp
var userInfo = await _authService.GetCurrentUserAsync(userId);

// 用户基本信息
Console.WriteLine($"用户: {userInfo.Name} ({userInfo.Account})");
Console.WriteLine($"邮箱: {userInfo.Email}");

// 用户角色
Console.WriteLine("角色:");
foreach (var role in userInfo.Roles)
{
    Console.WriteLine($"  - {role.Name}");
}

// 用户权限
Console.WriteLine("权限:");
foreach (var permission in userInfo.Permissions)
{
    Console.WriteLine($"  - {permission}");
}

// 用户菜单
Console.WriteLine("菜单:");
PrintMenuTree(userInfo.Menus);
```

### 5.3 修改密码

```csharp
try
{
    await _authService.ChangePasswordAsync(userId, "oldPassword", "newPassword");
    Console.WriteLine("密码修改成功");
}
catch (DomainException ex)
{
    Console.WriteLine($"密码修改失败: {ex.Message}");
}
```

## 6. 完整业务场景示例

### 6.1 用户注册流程

```csharp
public async Task<UserDto> RegisterUser(string name, string account, string password, string email)
{
    // 1. 创建用户
    var createDto = new UserCreateDto
    {
        Name = name,
        Account = account,
        Password = password,
        Email = email
    };
    
    var user = await _userService.CreateAsync(createDto);
    
    // 2. 分配默认角色（普通用户）
    var defaultRole = await _roleRepository.FindByCodeAsync("User");
    if (defaultRole != null)
    {
        await _userService.AssignRolesAsync(user.Id, new List<Guid> { defaultRole.Id });
    }
    
    return user;
}
```

### 6.2 权限验证流程

```csharp
public async Task<bool> CanAccessResource(Guid userId, string resourceCode)
{
    // 1. 获取用户权限
    var permissions = await _userService.GetUserPermissionsAsync(userId);
    
    // 2. 检查是否有对应权限
    return permissions.Contains(resourceCode);
}
```

### 6.3 动态菜单加载

```csharp
public async Task<List<MenuTreeDto>> LoadUserMenus(Guid userId)
{
    // 1. 获取用户菜单（已根据权限过滤）
    var menus = await _menuService.GetUserMenusAsync(userId);
    
    // 2. 过滤隐藏菜单
    var visibleMenus = FilterHiddenMenus(menus);
    
    return visibleMenus;
}

private List<MenuTreeDto> FilterHiddenMenus(List<MenuTreeDto> menus)
{
    var result = new List<MenuTreeDto>();
    
    foreach (var menu in menus.Where(m => !m.IsHidden))
    {
        var menuCopy = new MenuTreeDto
        {
            Id = menu.Id,
            Name = menu.Name,
            Icon = menu.Icon,
            Route = menu.Route,
            Component = menu.Component,
            Children = FilterHiddenMenus(menu.Children)
        };
        
        result.Add(menuCopy);
    }
    
    return result;
}
```

### 6.4 角色权限管理

```csharp
public async Task SetupEditorRole()
{
    // 1. 创建编辑员角色
    var roleDto = new RoleCreateDto
    {
        Name = "编辑员",
        Code = "Editor"
    };
    
    var role = await _roleService.CreateAsync(roleDto);
    
    // 2. 分配权限
    var permissions = new List<string>
    {
        "Rbac.Users.Query",
        "Rbac.Users.Update",
        "Rbac.Roles.Query"
    };
    
    await _roleService.AssignPermissionsAsync(role.Id, permissions);
    
    // 3. 创建对应菜单
    var menuDto = new MenuCreateDto
    {
        Name = "编辑管理",
        Type = MenuType.Menu,
        PermissionCode = "Rbac.Users.Update",
        Icon = "edit",
        Route = "/editor",
        Component = "editor/index"
    };
    
    await _menuService.CreateAsync(menuDto);
}
```

## 7. 错误处理示例

```csharp
public async Task<IActionResult> CreateUser(UserCreateDto input)
{
    try
    {
        var user = await _userService.CreateAsync(input);
        return Ok(user);
    }
    catch (DomainException ex)
    {
        // 领域异常（如账号已存在）
        return BadRequest(new { message = ex.Message });
    }
    catch (ValidationException ex)
    {
        // 验证异常
        return BadRequest(new { message = "数据验证失败", errors = ex.Errors });
    }
    catch (Exception ex)
    {
        // 其他异常
        _logger.LogError(ex, "创建用户失败");
        return StatusCode(500, new { message = "服务器内部错误" });
    }
}
```

## 8. 性能优化示例

### 8.1 批量操作

```csharp
// 批量创建用户
public async Task<List<UserDto>> BatchCreateUsers(List<UserCreateDto> inputs)
{
    var users = new List<UserDto>();
    
    foreach (var input in inputs)
    {
        var user = await _userService.CreateAsync(input);
        users.Add(user);
    }
    
    return users;
}
```

### 8.2 缓存权限

```csharp
public async Task<List<string>> GetUserPermissionsWithCache(Guid userId)
{
    var cacheKey = $"user:{userId}:permissions";
    
    // 尝试从缓存获取
    var cached = await _cache.GetAsync<List<string>>(cacheKey);
    if (cached != null)
    {
        return cached;
    }
    
    // 从数据库查询
    var permissions = await _userService.GetUserPermissionsAsync(userId);
    
    // 写入缓存（30分钟过期）
    await _cache.SetAsync(cacheKey, permissions, TimeSpan.FromMinutes(30));
    
    return permissions;
}
```

## 9. 测试示例

```csharp
[Fact]
public async Task Should_Create_User_Successfully()
{
    // Arrange
    var input = new UserCreateDto
    {
        Name = "测试用户",
        Account = "test001",
        Password = "Pass@123",
        Email = "test@example.com"
    };
    
    // Act
    var result = await _userService.CreateAsync(input);
    
    // Assert
    Assert.NotNull(result);
    Assert.Equal(input.Name, result.Name);
    Assert.Equal(input.Account, result.Account);
}

[Fact]
public async Task Should_Throw_Exception_When_Account_Exists()
{
    // Arrange
    var input = new UserCreateDto
    {
        Name = "测试用户",
        Account = "admin", // 已存在
        Password = "Pass@123"
    };
    
    // Act & Assert
    await Assert.ThrowsAsync<DomainException>(
        () => _userService.CreateAsync(input)
    );
}
```

## 10. 集成示例

### 10.1 与JWT集成

```csharp
public async Task<string> LoginAndGenerateToken(string account, string password)
{
    // 1. 验证用户
    var userInfo = await _authService.LoginAsync(account, password);
    
    // 2. 生成JWT Token
    var claims = new List<Claim>
    {
        new Claim(ClaimTypes.NameIdentifier, userInfo.Id.ToString()),
        new Claim(ClaimTypes.Name, userInfo.Name),
        new Claim(ClaimTypes.Email, userInfo.Email ?? "")
    };
    
    // 添加角色声明
    foreach (var role in userInfo.Roles)
    {
        claims.Add(new Claim(ClaimTypes.Role, role.Code));
    }
    
    // 添加权限声明
    foreach (var permission in userInfo.Permissions)
    {
        claims.Add(new Claim("Permission", permission));
    }
    
    var token = _jwtService.GenerateToken(claims);
    return token;
}
```

### 10.2 与SignalR集成

```csharp
public class NotificationHub : Hub
{
    private readonly IUserService _userService;
    
    public override async Task OnConnectedAsync()
    {
        var userId = Guid.Parse(Context.UserIdentifier);
        
        // 获取用户权限
        var permissions = await _userService.GetUserPermissionsAsync(userId);
        
        // 根据权限加入不同的组
        if (permissions.Contains("Rbac.Users"))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, "UserManagers");
        }
        
        await base.OnConnectedAsync();
    }
}
```

这些示例涵盖了RBAC模块的主要使用场景，可以根据实际需求进行调整和扩展。
