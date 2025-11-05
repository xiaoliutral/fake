using Fake.Domain.Repositories;

namespace Fake.Rbac.Domain.RoleAggregate;

public interface IRoleRepository: IRepository<Role>
{
    /// <summary>
    /// 根据编码查找角色
    /// </summary>
    Task<Role?> FindByCodeAsync(string code, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 获取角色及其权限
    /// </summary>
    Task<Role?> GetWithPermissionsAsync(Guid roleId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 批量获取角色及其权限
    /// </summary>
    Task<List<Role>> GetRolesWithPermissionsAsync(List<Guid> roleIds, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 检查角色编码是否已存在
    /// </summary>
    Task<bool> IsCodeExistsAsync(string code, Guid? excludeRoleId = null, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 获取角色的用户数量
    /// </summary>
    Task<int> GetUserCountAsync(Guid roleId, CancellationToken cancellationToken = default);
}