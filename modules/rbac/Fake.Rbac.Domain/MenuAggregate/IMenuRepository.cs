using Fake.Domain.Repositories;

namespace Fake.Rbac.Domain.MenuAggregate;

public interface IMenuRepository: IRepository<Menu>
{
    public Task<IQueryable<Menu>> GetQueryableAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 获取菜单树
    /// </summary>
    Task<List<Menu>> GetMenuTreeAsync(Guid? parentId = null, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 根据权限代码获取菜单
    /// </summary>
    Task<List<Menu>> GetMenusByPermissionsAsync(List<string> permissionCodes, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 获取所有父级菜单
    /// </summary>
    Task<List<Menu>> GetParentMenusAsync(Guid menuId, CancellationToken cancellationToken = default);
}