using Fake.Rbac.Application.Dtos.Common;

namespace Fake.Rbac.Application.Dtos.User;

/// <summary>
/// 用户简单 DTO
/// </summary>
public class UserSimpleDto : EntityDto<Guid>
{
    /// <summary>
    /// 名称
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// 账号
    /// </summary>
    public string Account { get; set; } = string.Empty;
    
    /// <summary>
    /// 头像
    /// </summary>
    public string? Avatar { get; set; }
}

