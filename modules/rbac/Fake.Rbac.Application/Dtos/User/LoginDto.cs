using System.ComponentModel.DataAnnotations;

namespace Fake.Rbac.Application.Dtos.User;

/// <summary>
/// 登录 DTO
/// </summary>
public class LoginDto
{
    /// <summary>
    /// 账号
    /// </summary>
    [Required(ErrorMessage = "账号不能为空")]
    public string Account { get; set; } = string.Empty;
    
    /// <summary>
    /// 密码
    /// </summary>
    [Required(ErrorMessage = "密码不能为空")]
    public string Password { get; set; } = string.Empty;
}
