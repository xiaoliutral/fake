using Fake.Application;
using Fake.Rbac.Application.Dtos.Permission;
using Fake.Rbac.Domain.MenuAggregate;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Fake.Rbac.Application.Services;

[ApiExplorerSettings(GroupName = "RBAC")]
public class PermissionService : ApplicationService, IPermissionService
{
    private readonly IUserService _userService;
    private readonly IMenuRepository _menuRepository;

    public PermissionService(
        IUserService userService,
        IMenuRepository menuRepository)
    {
        _userService = userService;
        _menuRepository = menuRepository;
    }

    public async Task<List<PermissionDefinitionDto>> GetAllPermissionsAsync(CancellationToken cancellationToken = default)
    {
        // 从菜单表读取所有菜单（菜单就是权限）
        var queryable = await _menuRepository.GetQueryableAsync(cancellationToken);
        var menus = await queryable.ToListAsync(cancellationToken);
        
        var dtos = menus
            .Where(m => !string.IsNullOrEmpty(m.PermissionCode))
            .Select(m => new PermissionDefinitionDto
            {
                Code = m.PermissionCode!,
                Name = m.Name,
                ParentCode = GetParentPermissionCode(m, menus),
                Description = m.Description
            })
            .ToList();
        
        return dtos;
    }
    
    private string? GetParentPermissionCode(Menu menu, List<Menu> allMenus)
    {
        if (menu.PId == Guid.Empty)
        {
            return null;
        }
        
        var parent = allMenus.FirstOrDefault(m => m.Id == menu.PId);
        return parent?.PermissionCode;
    }

    public async Task<List<PermissionGroupDto>> GetPermissionTreeAsync(CancellationToken cancellationToken = default)
    {
        var queryable = await _menuRepository.GetQueryableAsync(cancellationToken);
        var menus = await queryable.OrderBy(m => m.Order).ToListAsync(cancellationToken);
        
        // 找到所有顶级菜单（作为权限分组）
        var rootMenus = menus.Where(m => m.PId == Guid.Empty && !string.IsNullOrEmpty(m.PermissionCode)).ToList();
        
        var groups = new List<PermissionGroupDto>();
        
        foreach (var root in rootMenus)
        {
            var group = new PermissionGroupDto
            {
                Code = root.PermissionCode!,
                Name = root.Name,
                Permissions = BuildPermissionTreeFromMenus(root.Id, menus)
            };
            groups.Add(group);
        }

        return groups;
    }
    
    private List<PermissionDefinitionDto> BuildPermissionTreeFromMenus(Guid parentId, List<Menu> allMenus)
    {
        var result = new List<PermissionDefinitionDto>();
        var children = allMenus
            .Where(m => m.PId == parentId && !string.IsNullOrEmpty(m.PermissionCode))
            .OrderBy(m => m.Order)
            .ToList();

        foreach (var child in children)
        {
            var dto = new PermissionDefinitionDto
            {
                Code = child.PermissionCode!,
                Name = child.Name,
                ParentCode = allMenus.FirstOrDefault(m => m.Id == child.PId)?.PermissionCode,
                Description = child.Description,
                // 递归构建子权限树
                Children = BuildPermissionTreeFromMenus(child.Id, allMenus)
            };
            result.Add(dto);
        }

        return result;
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


}

