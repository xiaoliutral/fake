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
    public string PermissionCode { get; private set; } = null!;
    
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
    
    public Menu()
    {
        
    }
    
    public Menu(Guid pId, string name, MenuType type, string? permissionCode = null, string? icon = null, 
        string? route = null, string? component = null, int order = 0, bool isHidden = false, 
        bool isCached = false, string? description = null)
    {
        PId = pId;
        Name = name;
        Type = type;
        PermissionCode = permissionCode;
        Icon = icon;
        Route = route;
        Component = component;
        Order = order;
        IsHidden = isHidden;
        IsCached = isCached;
        Description = description;
    }
    
    public void Update(string? name = null, string? permissionCode = null, string? icon = null, 
        string? route = null, string? component = null, bool? isHidden = null, 
        bool? isCached = null, string? description = null)
    {
        if (!string.IsNullOrWhiteSpace(name))
        {
            Name = name;
        }

        if (permissionCode != null)
        {
            PermissionCode = permissionCode;
        }

        if (icon != null)
        {
            Icon = icon;
        }

        if (route != null)
        {
            Route = route;
        }

        if (component != null)
        {
            Component = component;
        }

        if (isHidden.HasValue)
        {
            IsHidden = isHidden.Value;
        }

        if (isCached.HasValue)
        {
            IsCached = isCached.Value;
        }

        if (description != null)
        {
            Description = description;
        }
    }

    public void UpdateOrder(int order)
    {
        Order = order;
    }

    public void MoveTo(Guid? parentId)
    {
        PId = parentId ?? Guid.Empty;
    }
    
    // add sub menu
    public void AddChild(Menu menu)
    {
        _children.Add(menu);
    }
    
    // rem sub menu
    public void RemoveChild(Menu menu)
    {
        _children.Remove(menu);
    }
}