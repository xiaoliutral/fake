using Fake.Rbac.Application.Dtos.Common;

namespace Fake.Rbac.Application.Dtos.Role;

/// <summary>
/// 角色分页查询 DTO
/// </summary>
public class RolePagedRequestDto : PagedRequestDto
{
    /// <summary>
    /// 关键字（搜索名称或编码）
    /// </summary>
    public string? Keyword { get; set; }
}

