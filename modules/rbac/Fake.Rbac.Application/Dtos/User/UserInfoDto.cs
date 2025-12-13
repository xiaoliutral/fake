using Fake.Rbac.Application.Dtos.Menu;

namespace Fake.Rbac.Application.Dtos.User;

/// <summary>
/// 用户信息 DTO（包含权限和菜单）
/// </summary>
public class UserInfoDto : UserDto
{
    /// <summary>
    /// 权限列表
    /// </summary>
    public List<string> Permissions { get; set; } = new();
    
    /// <summary>
    /// 菜单列表
    /// </summary>
    public List<MenuTreeDto> Menus { get; set; } = new();
}
