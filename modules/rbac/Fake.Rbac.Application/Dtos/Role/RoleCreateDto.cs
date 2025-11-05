using System.ComponentModel.DataAnnotations;

namespace Fake.Rbac.Application.Dtos.Role;

/// <summary>
/// 创建角色 DTO
/// </summary>
public class RoleCreateDto
{
    /// <summary>
    /// 名称
    /// </summary>
    [Required(ErrorMessage = "名称不能为空")]
    [StringLength(50, ErrorMessage = "名称长度不能超过50")]
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// 编码
    /// </summary>
    [Required(ErrorMessage = "编码不能为空")]
    [StringLength(50, ErrorMessage = "编码长度不能超过50")]
    [RegularExpression(@"^[a-zA-Z][a-zA-Z0-9_]*$", ErrorMessage = "编码必须以字母开头，只能包含字母、数字和下划线")]
    public string Code { get; set; } = string.Empty;
    
    /// <summary>
    /// 权限列表
    /// </summary>
    public List<string>? Permissions { get; set; }
}

