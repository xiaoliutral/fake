using Fake.Rbac.Application.Dtos.Menu;

namespace Fake.Rbac.Application.Services;

/// <summary>
/// 菜单服务接口
/// </summary>
public interface IMenuService
{
    /// <summary>
    /// 获取菜单详情
    /// </summary>
    Task<MenuDto> GetAsync(Guid id, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 获取菜单树
    /// </summary>
    Task<List<MenuTreeDto>> GetMenuTreeAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 获取用户菜单
    /// </summary>
    Task<List<MenuTreeDto>> GetUserMenusAsync(Guid userId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 创建菜单
    /// </summary>
    Task<MenuDto> CreateAsync(MenuCreateDto input, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 更新菜单
    /// </summary>
    Task<MenuDto> UpdateAsync(Guid id, MenuUpdateDto input, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 更新排序
    /// </summary>
    Task UpdateOrderAsync(Guid id, int order, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 删除菜单
    /// </summary>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 移动菜单
    /// </summary>
    Task MoveMenuAsync(Guid menuId, Guid? targetParentId, CancellationToken cancellationToken = default);
}

