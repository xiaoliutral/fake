namespace Fake.Rbac.Domain.Permissions;

/// <summary>
/// 权限定义提供器接口
/// </summary>
public interface IPermissionDefinitionProvider
{
    /// <summary>
    /// 获取所有权限定义
    /// </summary>
    List<PermissionDefinition> GetPermissions();
}
