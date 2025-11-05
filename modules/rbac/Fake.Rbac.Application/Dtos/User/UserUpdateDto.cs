using System.ComponentModel.DataAnnotations;

namespace Fake.Rbac.Application.Dtos.User;

/// <summary>
/// 更新用户 DTO
/// </summary>
public class UserUpdateDto
{
    /// <summary>
    /// 名称
    /// </summary>
    [StringLength(50, ErrorMessage = "名称长度不能超过50")]
    public string? Name { get; set; }
    
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
}

