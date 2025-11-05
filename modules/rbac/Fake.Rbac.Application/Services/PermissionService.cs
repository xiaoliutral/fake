using Fake.Application;
using Fake.Rbac.Application.Dtos.Permission;

namespace Fake.Rbac.Application.Services;

public class PermissionService : ApplicationService, IPermissionService
{
    private readonly IUserService _userService;

    public PermissionService(IUserService userService)
    {
        _userService = userService;
    }

    public Task<List<PermissionDefinitionDto>> GetAllPermissionsAsync(CancellationToken cancellationToken = default)
    {
        // TODO: 从权限定义提供器中获取所有权限
        // 这里先返回硬编码的权限列表，后续需要实现权限定义提供器
        var permissions = new List<PermissionDefinitionDto>
        {
            new() { Code = "Rbac", Name = "权限管理", Description = "权限管理模块" },
            new() { Code = "Rbac.Users", Name = "用户管理", ParentCode = "Rbac", Description = "用户管理功能" },
            new() { Code = "Rbac.Users.Query", Name = "查询用户", ParentCode = "Rbac.Users" },
            new() { Code = "Rbac.Users.Create", Name = "创建用户", ParentCode = "Rbac.Users" },
            new() { Code = "Rbac.Users.Update", Name = "更新用户", ParentCode = "Rbac.Users" },
            new() { Code = "Rbac.Users.Delete", Name = "删除用户", ParentCode = "Rbac.Users" },
            
            new() { Code = "Rbac.Roles", Name = "角色管理", ParentCode = "Rbac", Description = "角色管理功能" },
            new() { Code = "Rbac.Roles.Query", Name = "查询角色", ParentCode = "Rbac.Roles" },
            new() { Code = "Rbac.Roles.Create", Name = "创建角色", ParentCode = "Rbac.Roles" },
            new() { Code = "Rbac.Roles.Update", Name = "更新角色", ParentCode = "Rbac.Roles" },
            new() { Code = "Rbac.Roles.Delete", Name = "删除角色", ParentCode = "Rbac.Roles" },
            new() { Code = "Rbac.Roles.AssignPermissions", Name = "分配权限", ParentCode = "Rbac.Roles" },
            
            new() { Code = "Rbac.Menus", Name = "菜单管理", ParentCode = "Rbac", Description = "菜单管理功能" },
            new() { Code = "Rbac.Menus.Query", Name = "查询菜单", ParentCode = "Rbac.Menus" },
            new() { Code = "Rbac.Menus.Create", Name = "创建菜单", ParentCode = "Rbac.Menus" },
            new() { Code = "Rbac.Menus.Update", Name = "更新菜单", ParentCode = "Rbac.Menus" },
            new() { Code = "Rbac.Menus.Delete", Name = "删除菜单", ParentCode = "Rbac.Menus" },
        };

        return Task.FromResult(permissions);
    }

    public async Task<List<PermissionGroupDto>> GetPermissionTreeAsync(CancellationToken cancellationToken = default)
    {
        var allPermissions = await GetAllPermissionsAsync(cancellationToken);

        // 按模块分组
        var groups = new List<PermissionGroupDto>();

        var rootPermissions = allPermissions.Where(p => string.IsNullOrEmpty(p.ParentCode)).ToList();

        foreach (var root in rootPermissions)
        {
            var group = new PermissionGroupDto
            {
                Name = root.Name,
                Permissions = BuildPermissionTree(root.Code, allPermissions)
            };
            groups.Add(group);
        }

        return groups;
    }

    public async Task<bool> CheckPermissionAsync(Guid userId, string permissionCode, CancellationToken cancellationToken = default)
    {
        return await _userService.HasPermissionAsync(userId, permissionCode, cancellationToken);
    }

    public async Task<Dictionary<string, bool>> CheckPermissionsAsync(Guid userId, List<string> permissionCodes, CancellationToken cancellationToken = default)
    {
        var userPermissions = await _userService.GetUserPermissionsAsync(userId, cancellationToken);
        var result = new Dictionary<string, bool>();

        foreach (var code in permissionCodes)
        {
            result[code] = userPermissions.Contains(code);
        }

        return result;
    }

    private List<PermissionDefinitionDto> BuildPermissionTree(string parentCode, List<PermissionDefinitionDto> allPermissions)
    {
        var result = new List<PermissionDefinitionDto>();
        var children = allPermissions.Where(p => p.ParentCode == parentCode).ToList();

        foreach (var child in children)
        {
            result.Add(child);
            result.AddRange(BuildPermissionTree(child.Code, allPermissions));
        }

        return result;
    }
}

