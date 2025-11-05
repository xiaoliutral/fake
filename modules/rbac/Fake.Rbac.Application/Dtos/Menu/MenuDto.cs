using Fake.Rbac.Application.Dtos.Common;
using Fake.Rbac.Domain.MenuAggregate;

namespace Fake.Rbac.Application.Dtos.Menu;

/// <summary>
/// 菜单 DTO
/// </summary>
public class MenuDto : AuditedEntityDto<Guid>
{
    /// <summary>
    /// 父级菜单ID
    /// </summary>
    public Guid PId { get; set; }
    
    /// <summary>
    /// 名称
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// 权限代码
    /// </summary>
    public string? PermissionCode { get; set; }
    
    /// <summary>
    /// 菜单类型
    /// </summary>
    public MenuType Type { get; set; }
    
    /// <summary>
    /// 图标
    /// </summary>
    public string? Icon { get; set; }
    
    /// <summary>
    /// 路由
    /// </summary>
    public string? Route { get; set; }
    
    /// <summary>
    /// 组件
    /// </summary>
    public string? Component { get; set; }
    
    /// <summary>
    /// 排序
    /// </summary>
    public int Order { get; set; }
    
    /// <summary>
    /// 是否隐藏
    /// </summary>
    public bool IsHidden { get; set; }
    
    /// <summary>
    /// 是否缓存
    /// </summary>
    public bool IsCached { get; set; }
    
    /// <summary>
    /// 描述
    /// </summary>
    public string? Description { get; set; }
}

