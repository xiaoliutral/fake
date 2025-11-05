using Fake.Rbac.Application.Dtos.Common;
using Fake.Rbac.Application.Dtos.Role;
using Fake.Rbac.Application.Dtos.User;

namespace Fake.Rbac.Application.Services;

/// <summary>
/// 角色服务接口
/// </summary>
public interface IRoleService
{
    /// <summary>
    /// 获取角色详情
    /// </summary>
    Task<RoleDto> GetAsync(Guid id, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 获取角色列表（分页）
    /// </summary>
    Task<PagedResultDto<RoleDto>> GetListAsync(RolePagedRequestDto input, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 获取所有角色（不分页）
    /// </summary>
    Task<List<RoleSimpleDto>> GetAllRolesAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 创建角色
    /// </summary>
    Task<RoleDto> CreateAsync(RoleCreateDto input, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 更新角色
    /// </summary>
    Task<RoleDto> UpdateAsync(Guid id, RoleUpdateDto input, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 删除角色
    /// </summary>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 分配权限
    /// </summary>
    Task AssignPermissionsAsync(Guid roleId, List<string> permissionCodes, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 获取角色权限
    /// </summary>
    Task<List<string>> GetRolePermissionsAsync(Guid roleId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 获取角色用户列表
    /// </summary>
    Task<List<UserSimpleDto>> GetRoleUsersAsync(Guid roleId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 获取角色用户数量
    /// </summary>
    Task<int> GetRoleUserCountAsync(Guid roleId, CancellationToken cancellationToken = default);
}

