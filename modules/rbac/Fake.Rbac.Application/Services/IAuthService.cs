using Fake.Rbac.Application.Dtos.User;

namespace Fake.Rbac.Application.Services;

/// <summary>
/// 认证服务接口
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// 用户登录
    /// </summary>
    Task<UserInfoDto> LoginAsync(string account, string password, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 获取当前用户信息
    /// </summary>
    Task<UserInfoDto> GetCurrentUserAsync(Guid userId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 修改当前用户密码
    /// </summary>
    Task ChangePasswordAsync(Guid userId, string oldPassword, string newPassword, CancellationToken cancellationToken = default);
}
