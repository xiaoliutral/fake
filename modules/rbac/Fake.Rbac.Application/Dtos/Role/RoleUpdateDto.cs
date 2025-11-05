using System.ComponentModel.DataAnnotations;

namespace Fake.Rbac.Application.Dtos.Role;

/// <summary>
/// 更新角色 DTO
/// </summary>
public class RoleUpdateDto
{
    /// <summary>
    /// 名称
    /// </summary>
    [StringLength(50, ErrorMessage = "名称长度不能超过50")]
    public string? Name { get; set; }
}

