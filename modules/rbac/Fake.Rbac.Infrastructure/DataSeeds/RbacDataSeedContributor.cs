using Fake.Data.Seeding;
using Fake.Rbac.Domain.MenuAggregate;
using Fake.Rbac.Domain.OrganizationAggregate;
using Fake.Rbac.Domain.RoleAggregate;
using Fake.Rbac.Domain.UserAggregate;
using Microsoft.EntityFrameworkCore;

namespace Fake.Rbac.Infrastructure.DataSeeds;

/// <summary>
/// RBAC模块数据种子
/// </summary>
public class RbacDataSeedContributor(FakeRbacDbContext dbContext, DbInitializer dbInitializer) : IDataSeedContributor
{
    public async Task SeedAsync()
    {
        await dbInitializer.InitializeAsync();
        
        // 种子数据：默认菜单（菜单就是权限）
        await SeedDefaultMenusAsync();
        
        // 种子数据：超级管理员角色
        await SeedAdminRoleAsync();
        
        // 种子数据：超级管理员用户
        await SeedAdminUserAsync();
        
        // 种子数据：默认组织架构
        await SeedDefaultOrganizationsAsync();
    }

    private async Task SeedAdminRoleAsync()
    {
        var adminRole = await dbContext.Roles
            .Include(r => r.Permissions)
            .FirstOrDefaultAsync(r => r.Code == "Admin");

        if (adminRole == null)
        {
            adminRole = new Role("超级管理员", "Admin");
            
            // 分配所有权限
            var allPermissions = new List<string>
            {
                "Rbac",
                "Rbac.Users", "Rbac.Users.Query", "Rbac.Users.Create", "Rbac.Users.Update", 
                "Rbac.Users.Delete", "Rbac.Users.AssignRoles", "Rbac.Users.ResetPassword",
                "Rbac.Roles", "Rbac.Roles.Query", "Rbac.Roles.Create", "Rbac.Roles.Update", 
                "Rbac.Roles.Delete", "Rbac.Roles.AssignPermissions",
                "Rbac.Menus", "Rbac.Menus.Query", "Rbac.Menus.Create", "Rbac.Menus.Update", 
                "Rbac.Menus.Delete", "Rbac.Menus.Move",
                "Rbac.Organizations", "Rbac.Organizations.Query", "Rbac.Organizations.Create", 
                "Rbac.Organizations.Update", "Rbac.Organizations.Delete", "Rbac.Organizations.Move"
            };

            adminRole.SetPermissions(allPermissions);
            
            await dbContext.Roles.AddAsync(adminRole);
            await dbContext.SaveChangesAsync();
        }
    }

    private async Task SeedAdminUserAsync()
    {
        var adminUser = await dbContext.Users
            .Include(u => u.Roles)
            .FirstOrDefaultAsync(u => u.Account == "admin");

        if (adminUser == null)
        {
            adminUser = new User("管理员", "admin", "123456", "admin@fake.com");
            
            // 分配超级管理员角色
            var adminRole = await dbContext.Roles.FirstOrDefaultAsync(r => r.Code == "Admin");
            if (adminRole != null)
            {
                adminUser.AssignRole(adminRole.Id);
            }
            
            await dbContext.Users.AddAsync(adminUser);
            await dbContext.SaveChangesAsync();
        }
    }

    private async Task SeedDefaultMenusAsync()
    {
        var existingMenus = await dbContext.Menus.AnyAsync();
        if (existingMenus)
        {
            return;
        }

        // 1. 先创建顶级菜单（系统管理）
        var systemMenu = new Menu(
            Guid.Empty, // 顶级菜单使用 Guid.Empty 作为父级
            "系统管理",
            MenuType.Menu,
            "Rbac",
            "setting",
            "/system",
            null,
            100,
            false,
            false,
            "系统管理模块"
        );
        await dbContext.Menus.AddAsync(systemMenu);
        await dbContext.SaveChangesAsync(); // 先保存顶级菜单

        // 2. 创建二级菜单
        var userMenu = new Menu(
            systemMenu.Id,
            "用户管理",
            MenuType.Menu,
            "Rbac.Users",
            "user",
            "/system/users",
            "system/users/index",
            1,
            false,
            true,
            "用户管理页面"
        );

        var roleMenu = new Menu(
            systemMenu.Id,
            "角色管理",
            MenuType.Menu,
            "Rbac.Roles",
            "team",
            "/system/roles",
            "system/roles/index",
            2,
            false,
            true,
            "角色管理页面"
        );

        var menuMenu = new Menu(
            systemMenu.Id,
            "菜单管理",
            MenuType.Menu,
            "Rbac.Menus",
            "menu",
            "/system/menus",
            "system/menus/index",
            3,
            false,
            true,
            "菜单管理页面"
        );

        var organizationMenu = new Menu(
            systemMenu.Id,
            "组织架构",
            MenuType.Menu,
            "Rbac.Organizations",
            "apartment",
            "/system/organizations",
            "system/organizations/index",
            4,
            false,
            true,
            "组织架构管理页面"
        );

        await dbContext.Menus.AddRangeAsync(new[] { userMenu, roleMenu, menuMenu, organizationMenu });
        await dbContext.SaveChangesAsync(); // 保存二级菜单

        // 3. 创建按钮（三级菜单）
        var buttons = new List<Menu>
        {
            // 用户管理按钮
            new Menu(userMenu.Id, "查询", MenuType.Button, "Rbac.Users.Query", null, null, null, 1),
            new Menu(userMenu.Id, "新增", MenuType.Button, "Rbac.Users.Create", null, null, null, 2),
            new Menu(userMenu.Id, "编辑", MenuType.Button, "Rbac.Users.Update", null, null, null, 3),
            new Menu(userMenu.Id, "删除", MenuType.Button, "Rbac.Users.Delete", null, null, null, 4),
            new Menu(userMenu.Id, "分配角色", MenuType.Button, "Rbac.Users.AssignRoles", null, null, null, 5),

            // 角色管理按钮
            new Menu(roleMenu.Id, "查询", MenuType.Button, "Rbac.Roles.Query", null, null, null, 1),
            new Menu(roleMenu.Id, "新增", MenuType.Button, "Rbac.Roles.Create", null, null, null, 2),
            new Menu(roleMenu.Id, "编辑", MenuType.Button, "Rbac.Roles.Update", null, null, null, 3),
            new Menu(roleMenu.Id, "删除", MenuType.Button, "Rbac.Roles.Delete", null, null, null, 4),
            new Menu(roleMenu.Id, "分配权限", MenuType.Button, "Rbac.Roles.AssignPermissions", null, null, null, 5),

            // 菜单管理按钮
            new Menu(menuMenu.Id, "查询", MenuType.Button, "Rbac.Menus.Query", null, null, null, 1),
            new Menu(menuMenu.Id, "新增", MenuType.Button, "Rbac.Menus.Create", null, null, null, 2),
            new Menu(menuMenu.Id, "编辑", MenuType.Button, "Rbac.Menus.Update", null, null, null, 3),
            new Menu(menuMenu.Id, "删除", MenuType.Button, "Rbac.Menus.Delete", null, null, null, 4),

            // 组织架构按钮
            new Menu(organizationMenu.Id, "查询", MenuType.Button, "Rbac.Organizations.Query", null, null, null, 1),
            new Menu(organizationMenu.Id, "新增", MenuType.Button, "Rbac.Organizations.Create", null, null, null, 2),
            new Menu(organizationMenu.Id, "编辑", MenuType.Button, "Rbac.Organizations.Update", null, null, null, 3),
            new Menu(organizationMenu.Id, "删除", MenuType.Button, "Rbac.Organizations.Delete", null, null, null, 4),
            new Menu(organizationMenu.Id, "移动", MenuType.Button, "Rbac.Organizations.Move", null, null, null, 5)
        };

        await dbContext.Menus.AddRangeAsync(buttons);
        await dbContext.SaveChangesAsync(); // 保存按钮
    }

    private async Task SeedDefaultOrganizationsAsync()
    {
        var existingOrganizations = await dbContext.Organizations.AnyAsync();
        if (existingOrganizations)
        {
            return;
        }

        // 创建示例组织架构
        var company = new Organization(
            "示例科技有限公司",
            "COMPANY001",
            OrganizationType.Company,
            null,
            null,
            1,
            "公司总部",
            true
        );
        await dbContext.Organizations.AddAsync(company);
        await dbContext.SaveChangesAsync();

        // 创建部门
        var techDept = new Organization(
            "技术部",
            "DEPT001",
            OrganizationType.Department,
            company.Id,
            null,
            1,
            "技术研发部门",
            true
        );

        var salesDept = new Organization(
            "销售部",
            "DEPT002",
            OrganizationType.Department,
            company.Id,
            null,
            2,
            "销售业务部门",
            true
        );

        var hrDept = new Organization(
            "人力资源部",
            "DEPT003",
            OrganizationType.Department,
            company.Id,
            null,
            3,
            "人力资源管理部门",
            true
        );

        await dbContext.Organizations.AddRangeAsync(new[] { techDept, salesDept, hrDept });
        await dbContext.SaveChangesAsync();

        // 创建小组
        var devGroup = new Organization(
            "开发组",
            "GROUP001",
            OrganizationType.Group,
            techDept.Id,
            null,
            1,
            "软件开发小组",
            true
        );

        var testGroup = new Organization(
            "测试组",
            "GROUP002",
            OrganizationType.Group,
            techDept.Id,
            null,
            2,
            "软件测试小组",
            true
        );

        await dbContext.Organizations.AddRangeAsync(new[] { devGroup, testGroup });
        await dbContext.SaveChangesAsync();
    }
}
