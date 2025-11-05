using System.ComponentModel.DataAnnotations;

namespace Fake.Rbac.Application.Dtos.User;

/// <summary>
/// 创建用户 DTO
/// </summary>
public class UserCreateDto
{
    /// <summary>
    /// 名称
    /// </summary>
    [Required(ErrorMessage = "名称不能为空")]
    [StringLength(50, ErrorMessage = "名称长度不能超过50")]
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// 账号
    /// </summary>
    [Required(ErrorMessage = "账号不能为空")]
    [StringLength(50, ErrorMessage = "账号长度不能超过50")]
    public string Account { get; set; } = string.Empty;
    
    /// <summary>
    /// 密码
    /// </summary>
    [Required(ErrorMessage = "密码不能为空")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "密码长度必须在6-100之间")]
    public string Password { get; set; } = string.Empty;
    
    /// <summary>
    /// 邮箱
    /// </summary>
    [EmailAddress(ErrorMessage = "邮箱格式不正确")]
    [StringLength(100, ErrorMessage = "邮箱长度不能超过100")]
    public string? Email { get; set; }
    
    /// <summary>
    /// 头像
    /// </summary>
    [StringLength(500, ErrorMessage = "头像URL长度不能超过500")]
    public string? Avatar { get; set; }
    
    /// <summary>
    /// 角色ID列表
    /// </summary>
    public List<Guid>? RoleIds { get; set; }
}

