using Fake.Rbac.Application.Dtos.Common;

namespace Fake.Rbac.Application.Dtos.Role;

/// <summary>
/// 角色简单 DTO
/// </summary>
public class RoleSimpleDto : EntityDto<Guid>
{
    /// <summary>
    /// 名称
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// 编码
    /// </summary>
    public string Code { get; set; } = string.Empty;
}

