using Fake.Rbac.Application.Dtos.Permission;

namespace Fake.Rbac.Application.Services;

/// <summary>
/// 权限服务接口
/// </summary>
public interface IPermissionService
{
    /// <summary>
    /// 获取所有权限定义
    /// </summary>
    Task<List<PermissionDefinitionDto>> GetAllPermissionsAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 获取权限树（按模块分组）
    /// </summary>
    Task<List<PermissionGroupDto>> GetPermissionTreeAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 检查权限
    /// </summary>
    Task<bool> CheckPermissionAsync(Guid userId, string permissionCode, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 批量检查权限
    /// </summary>
    Task<Dictionary<string, bool>> CheckPermissionsAsync(Guid userId, List<string> permissionCodes, CancellationToken cancellationToken = default);
}

