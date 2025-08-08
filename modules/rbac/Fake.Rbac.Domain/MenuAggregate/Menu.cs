using Fake.Domain.Entities.Auditing;

namespace Fake.Rbac.Domain.MenuAggregate;

public class Menu: FullAuditedAggregateRoot<Guid>
{
    /// <summary>
    /// 父级菜单
    /// </summary>
    public Guid PId { get; set; }
    
    public string Name { get; private set; }
    
    /// <summary>
    /// 权限代码
    /// </summary>
    public string? PermissionCode { get; private set; }
    
    /// <summary>
    /// 权限类型
    /// </summary>
    public MenuType Type { get; private set; }

    public string? Icon { get; private set; }

    /// <summary>
    /// 路由
    /// </summary>
    public string? Route { get; private set; }

    /// <summary>
    /// 组件
    /// </summary>
    public string? Component { get; private set; }

    /// <summary>
    /// 排序
    /// </summary>
    public int Order { get; private set; }

    /// <summary>
    /// 是否隐藏
    /// </summary>
    public bool IsHidden { get; private set; }

    /// <summary>
    /// 是否缓存
    /// </summary>
    public bool IsCached { get; private set; }
    
    public string? Description { get; private set; }


    private List<Menu> _children = new();
    public IReadOnlyList<Menu> Children => _children.AsReadOnly();
    
    // add sub menu
    public void AddChild(Menu menu)
    {
        _children.Add(menu);
    }
    
    // rem sub menu
    public void RemveChild(Menu menu)
    {
        _children.Remove(menu);
    }
}