using System.ComponentModel.DataAnnotations;
using Fake.Rbac.Domain.MenuAggregate;

namespace Fake.Rbac.Application.Dtos.Menu;

/// <summary>
/// 创建菜单 DTO
/// </summary>
public class MenuCreateDto
{
    /// <summary>
    /// 父级菜单ID
    /// </summary>
    public Guid? PId { get; set; }
    
    /// <summary>
    /// 名称
    /// </summary>
    [Required(ErrorMessage = "名称不能为空")]
    [StringLength(50, ErrorMessage = "名称长度不能超过50")]
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// 权限代码
    /// </summary>
    [StringLength(100, ErrorMessage = "权限代码长度不能超过100")]
    public string? PermissionCode { get; set; }
    
    /// <summary>
    /// 菜单类型
    /// </summary>
    [Required(ErrorMessage = "菜单类型不能为空")]
    public MenuType Type { get; set; }
    
    /// <summary>
    /// 图标
    /// </summary>
    [StringLength(100, ErrorMessage = "图标长度不能超过100")]
    public string? Icon { get; set; }
    
    /// <summary>
    /// 路由
    /// </summary>
    [StringLength(200, ErrorMessage = "路由长度不能超过200")]
    public string? Route { get; set; }
    
    /// <summary>
    /// 组件
    /// </summary>
    [StringLength(200, ErrorMessage = "组件长度不能超过200")]
    public string? Component { get; set; }
    
    /// <summary>
    /// 排序
    /// </summary>
    public int Order { get; set; } = 0;
    
    /// <summary>
    /// 是否隐藏
    /// </summary>
    public bool IsHidden { get; set; } = false;
    
    /// <summary>
    /// 是否缓存
    /// </summary>
    public bool IsCached { get; set; } = false;
    
    /// <summary>
    /// 描述
    /// </summary>
    [StringLength(500, ErrorMessage = "描述长度不能超过500")]
    public string? Description { get; set; }
}

