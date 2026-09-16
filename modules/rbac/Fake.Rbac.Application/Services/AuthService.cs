using Fake.Application;
using Fake.Domain.Exceptions;
using Fake.ObjectMapping;
using Fake.ObjectStorage;
using Fake.Rbac.Application.Dtos.Auth;
using Fake.Rbac.Application.Dtos.User;
using Fake.Rbac.Application.Jwt;
using Fake.Rbac.Domain.Managers;
using Fake.Rbac.Domain.UserAggregate;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;

namespace Fake.Rbac.Application.Services;

/// <summary>
/// 认证服务
/// </summary>
[Authorize]
[ApiExplorerSettings(GroupName = "RBAC")]
public class AuthService(
    AccountManager accountManager,
    UserService userService,
    MenuService menuService,
    IObjectMapper objectMapper,
    IJwtService jwtService,
    IUserRepository userRepository,
    IObjectStorage objectStorage)
    : ApplicationService
{
    [AllowAnonymous]
    public virtual async Task<LoginResultDto> LoginAsync(string account, string password, CancellationToken cancellationToken = default)
    {
        // 验证用户凭证
        var user = await accountManager.ValidateCredentialsAsync(account, password, cancellationToken);
        
        var claims = await jwtService.GenerateClaimsByUserIdAsync(user.Id, cancellationToken);
        
        // 生成 JWT Token
        var accessToken = jwtService.GenerateAccessToken(claims);
        var refreshToken = jwtService.GenerateRefreshToken(claims);
        
        return new LoginResultDto
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            TokenType = "Bearer",
            ExpiresIn = jwtService.GetExpiresInSeconds(),
            UserId = user.Id
        };
    }

    [AllowAnonymous]
    public virtual async Task<LoginResultDto> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        // 验证 RefreshToken
        var userIdStr = jwtService.ValidateRefreshToken(refreshToken);
        if (userIdStr != null)
        {
            throw new BusinessException("无效的刷新令牌");
        }

        if (Guid.TryParse(userIdStr, out var userId))
        {
            throw new BusinessException("无效的刷新令牌");
        }
        
        // 验证用户凭证
        var user = await userRepository.FirstAsync(x => x.Id == userId, cancellationToken);
        
        var claims = await jwtService.GenerateClaimsByUserIdAsync(user.Id, cancellationToken);
        
        // 生成新的 Token
        var newAccessToken = jwtService.GenerateAccessToken(claims);
        var newRefreshToken = jwtService.GenerateRefreshToken(claims);
        
        return new LoginResultDto
        {
            AccessToken = newAccessToken,
            RefreshToken = newRefreshToken,
            TokenType = "Bearer",
            ExpiresIn = jwtService.GetExpiresInSeconds(),
            UserId = userId
        };
    }

    public virtual async Task<UserInfoDto> GetCurrentUserAsync(CancellationToken cancellationToken = default)
    {
        var userId = CurrentUser.Id ?? throw new UnauthorizedAccessException("用户未登录");
        return await GetUserInfoAsync(userId, cancellationToken);
    }

    public virtual async Task ChangePasswordAsync(string oldPassword, string newPassword, CancellationToken cancellationToken = default)
    {
        var userId = CurrentUser.Id ?? throw new UnauthorizedAccessException("用户未登录");
        await accountManager.UpdatePasswordAsync(userId, oldPassword, newPassword, cancellationToken);
    }

    private async Task<UserInfoDto> GetUserInfoAsync(Guid userId, CancellationToken cancellationToken)
    {
        // 获取用户基本信息
        var userDto = await userService.GetAsync(userId, cancellationToken);
        
        // 获取用户权限
        var permissions = await userService.GetUserPermissionsAsync(userId, cancellationToken);
        
        // 获取用户菜单
        var menus = await menuService.GetUserMenusAsync(userId, cancellationToken);
        
        // 组装用户完整信息
        var userInfo = objectMapper.Map<UserDto, UserInfoDto>(userDto);
        userInfo.Permissions = permissions;
        userInfo.Menus = menus;
        
        return userInfo;
    }
    
    public virtual async Task<UserInfoDto> UpdateProfileAsync(string? name, string? email, CancellationToken cancellationToken = default)
    {
        var userId = CurrentUser.Id ?? throw new UnauthorizedAccessException("用户未登录");
        
        var user = await userRepository.FirstAsync(u => u.Id == userId, cancellationToken: cancellationToken);
        user.Update(name, email);
        await userRepository.UpdateAsync(user, cancellationToken: cancellationToken);
        await UnitOfWorkManager.Current!.SaveChangesAsync(cancellationToken);
        
        return await GetUserInfoAsync(userId, cancellationToken);
    }

    public virtual async Task<string> UploadAvatarAsync(IFormFile file, CancellationToken cancellationToken = default)
    {
        var userId = CurrentUser.Id ?? throw new UnauthorizedAccessException("用户未登录");
        
        // 验证文件
        if (file == null || file.Length == 0)
        {
            throw new DomainException("请选择要上传的文件");
        }
        
        // 验证文件类型
        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!allowedExtensions.Contains(extension))
        {
            throw new DomainException("只支持 jpg、jpeg、png、gif、webp 格式的图片");
        }
        
        // 验证文件大小（原始文件最大10MB）
        if (file.Length > 10 * 1024 * 1024)
        {
            throw new DomainException("文件大小不能超过10MB");
        }
        
        var objectKey = $"avatars/{userId}_{DateTime.Now:yyyyMMddHHmmss}.jpg";

        await using var output = new MemoryStream();
        await using (var stream = file.OpenReadStream())
        using (var image = await Image.LoadAsync(stream, cancellationToken))
        {
            var maxSize = 200;
            var width = image.Width;
            var height = image.Height;
            
            if (width > maxSize || height > maxSize)
            {
                if (width > height)
                {
                    height = (int)(height * ((float)maxSize / width));
                    width = maxSize;
                }
                else
                {
                    width = (int)(width * ((float)maxSize / height));
                    height = maxSize;
                }
            }
            
            image.Mutate(x => x.Resize(width, height));
            
            await image.SaveAsJpegAsync(output, new JpegEncoder { Quality = 80 }, cancellationToken);
        }

        output.Position = 0;
        var saveResult = await objectStorage.SaveAsync(new ObjectStorageSaveArgs
        {
            ObjectKey = objectKey,
            Content = output,
            ContentType = "image/jpeg"
        }, cancellationToken);
        
        var user = await userRepository.FirstAsync(u => u.Id == userId, cancellationToken: cancellationToken);
        user.UpdateAvatar(saveResult.Url);
        await userRepository.UpdateAsync(user, cancellationToken: cancellationToken);
        await UnitOfWorkManager.Current!.SaveChangesAsync(cancellationToken);
        
        return saveResult.Url;
    }
}
