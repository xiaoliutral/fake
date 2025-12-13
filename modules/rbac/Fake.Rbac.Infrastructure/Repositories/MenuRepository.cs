using Fake.DomainDrivenDesign.Repositories.EntityFrameWorkCore;
using Fake.EntityFrameworkCore;
using Fake.Rbac.Domain.MenuAggregate;
using Microsoft.EntityFrameworkCore;

namespace Fake.Rbac.Infrastructure.Repositories;

public class MenuRepository : EfCoreRepository<FakeRbacDbContext, Menu>, IMenuRepository
{
    public async Task<List<Menu>> GetMenuTreeAsync(Guid? parentId = null, CancellationToken cancellationToken = default)
    {
        var dbContext = await GetDbContextAsync(cancellationToken);
        var query = dbContext.Set<Menu>().AsQueryable();

        if (parentId.HasValue)
        {
            query = query.Where(m => m.PId == parentId.Value);
        }
        else
        {
            query = query.Where(m => m.PId == Guid.Empty);
        }

        var menus = await query
            .OrderBy(m => m.Order)
            .ToListAsync(cancellationToken);

        // 递归加载子菜单
        foreach (var menu in menus)
        {
            await LoadChildrenRecursiveAsync(dbContext, menu, cancellationToken);
        }

        return menus;
    }

    public async Task<List<Menu>> GetMenusByPermissionsAsync(List<string> permissionCodes, CancellationToken cancellationToken = default)
    {
        var dbContext = await GetDbContextAsync(cancellationToken);
        return await dbContext.Set<Menu>()
            .Where(m => m.PermissionCode != null && permissionCodes.Contains(m.PermissionCode))
            .OrderBy(m => m.Order)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Menu>> GetParentMenusAsync(Guid menuId, CancellationToken cancellationToken = default)
    {
        var dbContext = await GetDbContextAsync(cancellationToken);
        var parents = new List<Menu>();
        var menu = await dbContext.Set<Menu>().FindAsync(new object[] { menuId }, cancellationToken);

        while (menu != null && menu.PId != Guid.Empty)
        {
            var parent = await dbContext.Set<Menu>().FindAsync(new object[] { menu.PId }, cancellationToken);
            if (parent != null)
            {
                parents.Insert(0, parent);
                menu = parent;
            }
            else
            {
                break;
            }
        }

        return parents;
    }

    private async Task LoadChildrenRecursiveAsync(FakeRbacDbContext dbContext, Menu menu, CancellationToken cancellationToken)
    {
        var children = await dbContext.Set<Menu>()
            .Where(m => m.PId == menu.Id)
            .OrderBy(m => m.Order)
            .ToListAsync(cancellationToken);

        foreach (var child in children)
        {
            menu.AddChild(child);
            await LoadChildrenRecursiveAsync(dbContext, child, cancellationToken);
        }
    }
}

