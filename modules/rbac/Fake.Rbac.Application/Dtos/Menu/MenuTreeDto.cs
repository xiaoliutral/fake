namespace Fake.Rbac.Application.Dtos.Menu;

/// <summary>
/// 菜单树 DTO
/// </summary>
public class MenuTreeDto : MenuDto
{
    /// <summary>
    /// 子菜单列表
    /// </summary>
    public List<MenuTreeDto> Children { get; set; } = new();
}

