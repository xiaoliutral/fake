using Fake.Rbac.Application.Dtos.Common;
using Fake.Rbac.Application.Dtos.Role;
using Fake.Rbac.Application.Dtos.User;

namespace Fake.Rbac.Application.Services;

/// <summary>
/// 用户服务接口
/// </summary>
public interface IUserService
{
    /// <summary>
    /// 获取用户详情
    /// </summary>
    Task<UserDto> GetAsync(Guid id, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 获取用户列表（分页）
    /// </summary>
    Task<PagedResultDto<UserDto>> GetListAsync(UserPagedRequestDto input, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 根据角色获取用户列表
    /// </summary>
    Task<List<UserSimpleDto>> GetUsersByRoleAsync(Guid roleId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 创建用户
    /// </summary>
    Task<UserDto> CreateAsync(UserCreateDto input, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 更新用户
    /// </summary>
    Task<UserDto> UpdateAsync(Guid id, UserUpdateDto input, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 修改密码
    /// </summary>
    Task UpdatePasswordAsync(Guid id, UpdatePasswordDto input, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 更新头像
    /// </summary>
    Task UpdateAvatarAsync(Guid id, string avatarUrl, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 删除用户
    /// </summary>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 批量删除用户
    /// </summary>
    Task DeleteBatchAsync(List<Guid> ids, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 分配角色
    /// </summary>
    Task AssignRolesAsync(Guid userId, List<Guid> roleIds, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 获取用户角色
    /// </summary>
    Task<List<RoleDto>> GetUserRolesAsync(Guid userId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 获取用户权限
    /// </summary>
    Task<List<string>> GetUserPermissionsAsync(Guid userId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 检查用户是否拥有权限
    /// </summary>
    Task<bool> HasPermissionAsync(Guid userId, string permissionCode, CancellationToken cancellationToken = default);
}

