using Fake.Application;
using Fake.Domain.Exceptions;
using Fake.ObjectMapping;
using Fake.Rbac.Application.Dtos.Menu;
using Fake.Rbac.Domain.MenuAggregate;
using Fake.Rbac.Domain.UserAggregate;
using Fake.Rbac.Infrastructure.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Fake.Rbac.Application.Services;

[ApiExplorerSettings(GroupName = "RBAC")]
public class MenuService : ApplicationService, IMenuService
{
    private readonly IMenuRepository _menuRepository;
    private readonly IUserRepository _userRepository;
    private readonly IObjectMapper _objectMapper;
    private readonly IUserService _userService;

    public MenuService(
        IMenuRepository menuRepository,
        IUserRepository userRepository,
        IObjectMapper objectMapper,
        IUserService userService)
    {
        _menuRepository = menuRepository;
        _userRepository = userRepository;
        _objectMapper = objectMapper;
        _userService = userService;
    }

    public async Task<MenuDto> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var menu = await _menuRepository.FirstAsync(m => m.Id == id, cancellationToken: cancellationToken);
        return _objectMapper.Map<Menu, MenuDto>(menu);
    }

    public async Task<List<MenuTreeDto>> GetMenuTreeAsync(CancellationToken cancellationToken = default)
    {
        var menus = await _menuRepository.GetMenuTreeAsync(cancellationToken: cancellationToken);
        return BuildMenuTree(menus);
    }

    public async Task<List<MenuTreeDto>> GetUserMenusAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        // 获取用户的所有权限
        var permissions = await _userService.GetUserPermissionsAsync(userId, cancellationToken);

        if (!permissions.Any())
        {
            return new List<MenuTreeDto>();
        }

        // 根据权限获取菜单
        var menus = await _menuRepository.GetMenusByPermissionsAsync(permissions, cancellationToken);

        // 获取所有父级菜单（没有权限控制的父级菜单也要显示）
        var parentMenuIds = menus.Where(m => m.PId != Guid.Empty).Select(m => m.PId).Distinct().ToList();
        var allMenuIds = new HashSet<Guid>(menus.Select(m => m.Id));

        foreach (var parentId in parentMenuIds)
        {
            if (!allMenuIds.Contains(parentId))
            {
                var parentMenus = await _menuRepository.GetParentMenusAsync(parentId, cancellationToken);
                menus.AddRange(parentMenus);
            }
        }

        menus = menus.DistinctBy(m => m.Id).OrderBy(m => m.Order).ToList();

        return BuildMenuTree(menus);
    }

    public async Task<MenuDto> CreateAsync(MenuCreateDto input, CancellationToken cancellationToken = default)
    {
        var menu = _objectMapper.Map<MenuCreateDto, Menu>(input);
        
        await _menuRepository.InsertAsync(menu, cancellationToken: cancellationToken);

        return await GetAsync(menu.Id, cancellationToken);
    }

    public async Task<MenuDto> UpdateAsync(Guid id, MenuUpdateDto input, CancellationToken cancellationToken = default)
    {
        var menu = await _menuRepository.FirstAsync(m => m.Id == id, cancellationToken: cancellationToken);

        menu.Update(input.Name, input.PermissionCode, input.Icon, input.Route, input.Component, 
            input.IsHidden, input.IsCached, input.Description);

        await _menuRepository.UpdateAsync(menu, cancellationToken: cancellationToken);

        return await GetAsync(id, cancellationToken);
    }

    public async Task UpdateOrderAsync(Guid id, int order, CancellationToken cancellationToken = default)
    {
        var menu = await _menuRepository.FirstAsync(m => m.Id == id, cancellationToken: cancellationToken);

        menu.UpdateOrder(order);

        await _menuRepository.UpdateAsync(menu, cancellationToken: cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        // 检查是否有子菜单
        var children = await (await _menuRepository.GetQueryableAsync(cancellationToken))
            .Where(m => ((Menu)m).PId == id)
            .ToListAsync(cancellationToken);

        if (children.Any())
        {
            throw new DomainException($"该菜单下还有 {children.Count} 个子菜单，无法删除");
        }

        var menu = await _menuRepository.FirstAsync(m => m.Id == id, cancellationToken: cancellationToken);
        await _menuRepository.DeleteAsync(menu, cancellationToken: cancellationToken);
    }

    public async Task MoveMenuAsync(Guid menuId, Guid? targetParentId, CancellationToken cancellationToken = default)
    {
        var menu = await _menuRepository.FirstAsync(m => m.Id == menuId, cancellationToken: cancellationToken);

        // 不能移动到自己或自己的子菜单下
        if (targetParentId.HasValue)
        {
            var targetMenu = await _menuRepository.FirstAsync(m => m.Id == targetParentId.Value, cancellationToken: cancellationToken);
            var parents = await _menuRepository.GetParentMenusAsync(targetParentId.Value, cancellationToken);
            
            if (parents.Any(p => p.Id == menuId) || targetParentId.Value == menuId)
            {
                throw new DomainException("不能将菜单移动到自己或自己的子菜单下");
            }
        }

        menu.MoveTo(targetParentId);

        await _menuRepository.UpdateAsync(menu, cancellationToken: cancellationToken);
    }

    private List<MenuTreeDto> BuildMenuTree(List<Menu> menus)
    {
        var menuDtos = _objectMapper.Map<List<Menu>, List<MenuTreeDto>>(menus);
        var menuDict = menuDtos.ToDictionary(m => m.Id);
        var rootMenus = new List<MenuTreeDto>();

        foreach (var menu in menuDtos)
        {
            if (menu.PId == Guid.Empty || !menuDict.ContainsKey(menu.PId))
            {
                rootMenus.Add(menu);
            }
            else
            {
                var parent = menuDict[menu.PId];
                parent.Children.Add(menu);
            }
        }

        return rootMenus.OrderBy(m => m.Order).ToList();
    }
}

