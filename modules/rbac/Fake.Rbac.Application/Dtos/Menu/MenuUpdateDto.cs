using System.ComponentModel.DataAnnotations;

namespace Fake.Rbac.Application.Dtos.Menu;

/// <summary>
/// 更新菜单 DTO
/// </summary>
public class MenuUpdateDto
{
    /// <summary>
    /// 名称
    /// </summary>
    [StringLength(50, ErrorMessage = "名称长度不能超过50")]
    public string? Name { get; set; }
    
    /// <summary>
    /// 权限代码
    /// </summary>
    [StringLength(100, ErrorMessage = "权限代码长度不能超过100")]
    public string? PermissionCode { get; set; }
    
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
    /// 是否隐藏
    /// </summary>
    public bool? IsHidden { get; set; }
    
    /// <summary>
    /// 是否缓存
    /// </summary>
    public bool? IsCached { get; set; }
    
    /// <summary>
    /// 描述
    /// </summary>
    [StringLength(500, ErrorMessage = "描述长度不能超过500")]
    public string? Description { get; set; }
}

