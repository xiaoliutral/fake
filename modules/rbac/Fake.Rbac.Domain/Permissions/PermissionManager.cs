using Fake.Domain;

namespace Fake.Rbac.Domain.Permissions;

/// <summary>
/// 权限管理器
/// </summary>
public class PermissionManager : DomainService
{
    private readonly IEnumerable<IPermissionDefinitionProvider> _providers;
    private List<PermissionDefinition>? _cachedPermissions;

    public PermissionManager(IEnumerable<IPermissionDefinitionProvider> providers)
    {
        _providers = providers;
    }

    /// <summary>
    /// 获取所有权限定义
    /// </summary>
    public List<PermissionDefinition> GetAllPermissions()
    {
        if (_cachedPermissions == null)
        {
            _cachedPermissions = new List<PermissionDefinition>();
            foreach (var provider in _providers)
            {
                _cachedPermissions.AddRange(provider.GetPermissions());
            }
        }

        return _cachedPermissions;
    }

    /// <summary>
    /// 根据代码获取权限定义
    /// </summary>
    public PermissionDefinition? GetPermission(string code)
    {
        return GetAllPermissions().FirstOrDefault(p => p.Code == code);
    }

    /// <summary>
    /// 获取权限树
    /// </summary>
    public List<PermissionDefinition> GetPermissionTree()
    {
        var allPermissions = GetAllPermissions();
        return BuildTree(null, allPermissions);
    }

    /// <summary>
    /// 验证权限代码是否存在
    /// </summary>
    public bool IsPermissionExists(string code)
    {
        return GetAllPermissions().Any(p => p.Code == code);
    }

    /// <summary>
    /// 获取子权限
    /// </summary>
    public List<PermissionDefinition> GetChildPermissions(string parentCode)
    {
        return GetAllPermissions().Where(p => p.ParentCode == parentCode).ToList();
    }

    private List<PermissionDefinition> BuildTree(string? parentCode, List<PermissionDefinition> allPermissions)
    {
        return allPermissions
            .Where(p => p.ParentCode == parentCode)
            .OrderBy(p => p.Code)
            .ToList();
    }
}
