namespace Fake.Rbac.Domain.Permissions;

/// <summary>
/// RBAC模块权限定义提供器
/// </summary>
public class RbacPermissionDefinitionProvider : IPermissionDefinitionProvider
{
    public List<PermissionDefinition> GetPermissions()
    {
        return new List<PermissionDefinition>
        {
            // 权限管理模块
            new("Rbac", "权限管理", description: "权限管理模块"),
            
            // 用户管理
            new("Rbac.Users", "用户管理", "Rbac", "用户管理功能"),
            new("Rbac.Users.Query", "查询用户", "Rbac.Users", "查看用户列表和详情"),
            new("Rbac.Users.Create", "创建用户", "Rbac.Users", "创建新用户"),
            new("Rbac.Users.Update", "更新用户", "Rbac.Users", "更新用户信息"),
            new("Rbac.Users.Delete", "删除用户", "Rbac.Users", "删除用户"),
            new("Rbac.Users.AssignRoles", "分配角色", "Rbac.Users", "为用户分配角色"),
            new("Rbac.Users.ResetPassword", "重置密码", "Rbac.Users", "重置用户密码"),
            
            // 角色管理
            new("Rbac.Roles", "角色管理", "Rbac", "角色管理功能"),
            new("Rbac.Roles.Query", "查询角色", "Rbac.Roles", "查看角色列表和详情"),
            new("Rbac.Roles.Create", "创建角色", "Rbac.Roles", "创建新角色"),
            new("Rbac.Roles.Update", "更新角色", "Rbac.Roles", "更新角色信息"),
            new("Rbac.Roles.Delete", "删除角色", "Rbac.Roles", "删除角色"),
            new("Rbac.Roles.AssignPermissions", "分配权限", "Rbac.Roles", "为角色分配权限"),
            
            // 菜单管理
            new("Rbac.Menus", "菜单管理", "Rbac", "菜单管理功能"),
            new("Rbac.Menus.Query", "查询菜单", "Rbac.Menus", "查看菜单列表和详情"),
            new("Rbac.Menus.Create", "创建菜单", "Rbac.Menus", "创建新菜单"),
            new("Rbac.Menus.Update", "更新菜单", "Rbac.Menus", "更新菜单信息"),
            new("Rbac.Menus.Delete", "删除菜单", "Rbac.Menus", "删除菜单"),
            new("Rbac.Menus.Move", "移动菜单", "Rbac.Menus", "移动菜单位置"),
        };
    }
}
