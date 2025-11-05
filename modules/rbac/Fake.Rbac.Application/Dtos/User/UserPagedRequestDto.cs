using Fake.Rbac.Application.Dtos.Common;

namespace Fake.Rbac.Application.Dtos.User;

/// <summary>
/// 用户分页查询 DTO
/// </summary>
public class UserPagedRequestDto : PagedRequestDto
{
    /// <summary>
    /// 关键字（搜索名称或账号）
    /// </summary>
    public string? Keyword { get; set; }
    
    /// <summary>
    /// 角色ID
    /// </summary>
    public Guid? RoleId { get; set; }
}

