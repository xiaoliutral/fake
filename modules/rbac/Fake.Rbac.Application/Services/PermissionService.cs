using Fake.Application;
using Fake.ObjectMapping;
using Fake.Rbac.Application.Dtos.Permission;
using Fake.Rbac.Domain.Permissions;
using Microsoft.AspNetCore.Mvc;

namespace Fake.Rbac.Application.Services;

[ApiExplorerSettings(GroupName = "RBAC")]
public class PermissionService : ApplicationService, IPermissionService
{
    private readonly IUserService _userService;
    private readonly PermissionManager _permissionManager;
    private readonly IObjectMapper _objectMapper;

    public PermissionService(
        IUserService userService,
        PermissionManager permissionManager,
        IObjectMapper objectMapper)
    {
        _userService = userService;
        _permissionManager = permissionManager;
        _objectMapper = objectMapper;
    }

    public Task<List<PermissionDefinitionDto>> GetAllPermissionsAsync(CancellationToken cancellationToken = default)
    {
        var permissions = _permissionManager.GetAllPermissions();
        var dtos = _objectMapper.Map<List<PermissionDefinition>, List<PermissionDefinitionDto>>(permissions);
        return Task.FromResult(dtos);
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

