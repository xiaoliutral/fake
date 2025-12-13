using Fake.Application;
using Fake.ObjectMapping;
using Fake.Rbac.Application.Dtos.User;
using Fake.Rbac.Domain.Managers;
using Microsoft.AspNetCore.Mvc;

namespace Fake.Rbac.Application.Services;

/// <summary>
/// 认证服务
/// </summary>
[ApiExplorerSettings(GroupName = "RBAC")]
public class AuthService : ApplicationService, IAuthService
{
    private readonly AccountManager _accountManager;
    private readonly IUserService _userService;
    private readonly IMenuService _menuService;
    private readonly IObjectMapper _objectMapper;

    public AuthService(
        AccountManager accountManager,
        IUserService userService,
        IMenuService menuService,
        IObjectMapper objectMapper)
    {
        _accountManager = accountManager;
        _userService = userService;
        _menuService = menuService;
        _objectMapper = objectMapper;
    }

    public async Task<UserInfoDto> LoginAsync(string account, string password, CancellationToken cancellationToken = default)
    {
        // 验证用户凭证
        var user = await _accountManager.ValidateCredentialsAsync(account, password, cancellationToken);
        
        // 获取用户完整信息
        return await GetUserInfoAsync(user.Id, cancellationToken);
    }

    public async Task<UserInfoDto> GetCurrentUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await GetUserInfoAsync(userId, cancellationToken);
    }

    public async Task ChangePasswordAsync(Guid userId, string oldPassword, string newPassword, CancellationToken cancellationToken = default)
    {
        await _accountManager.UpdatePasswordAsync(userId, oldPassword, newPassword, cancellationToken);
    }

    private async Task<UserInfoDto> GetUserInfoAsync(Guid userId, CancellationToken cancellationToken)
    {
        // 获取用户基本信息
        var userDto = await _userService.GetAsync(userId, cancellationToken);
        
        // 获取用户权限
        var permissions = await _userService.GetUserPermissionsAsync(userId, cancellationToken);
        
        // 获取用户菜单
        var menus = await _menuService.GetUserMenusAsync(userId, cancellationToken);
        
        // 组装用户完整信息
        var userInfo = _objectMapper.Map<UserDto, UserInfoDto>(userDto);
        userInfo.Permissions = permissions;
        userInfo.Menus = menus;
        
        return userInfo;
    }
}
