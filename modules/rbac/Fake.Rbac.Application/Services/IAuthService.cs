using Fake.Rbac.Application.Dtos.Auth;
using Fake.Rbac.Application.Dtos.User;
using Microsoft.AspNetCore.Http;

namespace Fake.Rbac.Application.Services;

/// <summary>
/// 认证服务接口
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// 用户登录
    /// </summary>
    Task<LoginResultDto> LoginAsync(string account, string password, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 刷新令牌
    /// </summary>
    Task<LoginResultDto> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 获取当前用户信息
    /// </summary>
    Task<UserInfoDto> GetCurrentUserAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 修改当前用户密码
    /// </summary>
    Task ChangePasswordAsync(string oldPassword, string newPassword, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 更新当前用户信息（用户名、邮箱）
    /// </summary>
    Task<UserInfoDto> UpdateProfileAsync(string? name, string? email, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 上传头像
    /// </summary>
    Task<string> UploadAvatarAsync(IFormFile file, CancellationToken cancellationToken = default);
}
