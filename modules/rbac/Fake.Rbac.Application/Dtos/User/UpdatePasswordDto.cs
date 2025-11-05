using System.ComponentModel.DataAnnotations;

namespace Fake.Rbac.Application.Dtos.User;

/// <summary>
/// 修改密码 DTO
/// </summary>
public class UpdatePasswordDto
{
    /// <summary>
    /// 旧密码
    /// </summary>
    [Required(ErrorMessage = "旧密码不能为空")]
    public string OldPassword { get; set; } = string.Empty;
    
    /// <summary>
    /// 新密码
    /// </summary>
    [Required(ErrorMessage = "新密码不能为空")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "密码长度必须在6-100之间")]
    public string NewPassword { get; set; } = string.Empty;
}

