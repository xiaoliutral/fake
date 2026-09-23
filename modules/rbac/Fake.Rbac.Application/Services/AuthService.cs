using Fake.Application;
using Fake.Domain.Exceptions;
using Fake.FileManagement.Domain.FileAggregate;
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
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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
    IObjectStorage objectStorage,
    IStoredFileRepository storedFileRepository,
    IOptions<FakeObjectStorageOptions> objectStorageOptions)
    : ApplicationService
{
    [AllowAnonymous]
    public virtual async Task<LoginResultDto> LoginAsync(string account, string password, CancellationToken cancellationToken = default)
    {
        var user = await accountManager.ValidateCredentialsAsync(account, password, cancellationToken);

        var claims = await jwtService.GenerateClaimsByUserIdAsync(user.Id, cancellationToken);

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
        var userIdStr = jwtService.ValidateRefreshToken(refreshToken);
        if (userIdStr == null)
        {
            throw new BusinessException("无效的刷新令牌");
        }

        if (!Guid.TryParse(userIdStr, out var userId))
        {
            throw new BusinessException("无效的刷新令牌");
        }

        var user = await userRepository.FirstAsync(x => x.Id == userId, cancellationToken);

        var claims = await jwtService.GenerateClaimsByUserIdAsync(user.Id, cancellationToken);

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
        var userDto = await userService.GetAsync(userId, cancellationToken);
        var permissions = await userService.GetUserPermissionsAsync(userId, cancellationToken);
        var menus = await menuService.GetUserMenusAsync(userId, cancellationToken);

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

    /// <summary>
    /// 上传头像：库中存 FileId，返回可访问 URL。
    /// </summary>
    public virtual async Task<string> UploadAvatarAsync(IFormFile file, CancellationToken cancellationToken = default)
    {
        var userId = CurrentUser.Id ?? throw new UnauthorizedAccessException("用户未登录");

        if (file == null || file.Length == 0)
        {
            throw new DomainException("请选择要上传的文件");
        }

        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!allowedExtensions.Contains(extension))
        {
            throw new DomainException("只支持 jpg、jpeg、png、gif、webp 格式的图片");
        }

        if (file.Length > 10 * 1024 * 1024)
        {
            throw new DomainException("文件大小不能超过10MB");
        }

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

        var fileId = Guid.NewGuid().ToString();
        var saveResult = await objectStorage.SaveAsync(new ObjectStorageSaveArgs
        {
            ObjectKey = fileId,
            Content = output,
            ContentType = "image/jpeg"
        }, cancellationToken);

        var storedFile = new StoredFile(
            fileName: $"{userId}.jpg",
            status: StoredFileStatus.Available,
            size: saveResult.Size ?? output.Length,
            contentType: "image/jpeg");

        await storedFileRepository.InsertAsync(storedFile, cancellationToken: cancellationToken);

        var user = await userRepository.FirstAsync(u => u.Id == userId, cancellationToken: cancellationToken);
        var previousAvatar = user.Avatar;
        user.UpdateAvatar(fileId);
        await userRepository.UpdateAsync(user, cancellationToken: cancellationToken);
        await UnitOfWorkManager.Current!.SaveChangesAsync(cancellationToken);

        await TryDeletePreviousAvatarAsync(previousAvatar, cancellationToken);

        TimeSpan? expires = null;
        if (objectStorageOptions.Value.SignUrlsByDefault)
        {
            expires = TimeSpan.FromSeconds(objectStorageOptions.Value.DefaultSignedUrlExpiresSeconds);
        }

        return await objectStorage.GetUrlAsync(fileId, expires, cancellationToken);
    }

    private async Task TryDeletePreviousAvatarAsync(string? previousAvatar, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(previousAvatar, out var oldFileId))
        {
            return;
        }

        try
        {
            var old = await storedFileRepository.FirstOrDefaultAsync(
                x => x.Id == oldFileId,
                cancellationToken: cancellationToken);
            if (old == null)
            {
                return;
            }

            try
            {
                await objectStorage.DeleteAsync(old.ObjectKey, cancellationToken);
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "删除旧头像对象失败：{ObjectKey}", old.ObjectKey);
            }

            await storedFileRepository.DeleteAsync(old, cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "清理旧头像元数据失败：{FileId}", previousAvatar);
        }
    }
}
