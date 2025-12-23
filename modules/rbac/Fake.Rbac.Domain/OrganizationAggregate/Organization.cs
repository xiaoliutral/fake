using Fake.Domain.Entities.Auditing;

namespace Fake.Rbac.Domain.OrganizationAggregate;

/// <summary>
/// 组织机构实体
/// </summary>
public class Organization : FullAuditedAggregateRoot<Guid>
{
    /// <summary>
    /// 父级组织ID
    /// </summary>
    public Guid? ParentId { get; private set; }
    
    /// <summary>
    /// 组织名称
    /// </summary>
    public string Name { get; private set; }
    
    /// <summary>
    /// 组织编码
    /// </summary>
    public string Code { get; private set; }
    
    /// <summary>
    /// 组织类型（公司、部门、小组等）
    /// </summary>
    public OrganizationType Type { get; private set; }
    
    /// <summary>
    /// 负责人ID
    /// </summary>
    public Guid? LeaderId { get; private set; }
    
    /// <summary>
    /// 排序
    /// </summary>
    public int Order { get; private set; }
    
    /// <summary>
    /// 描述
    /// </summary>
    public string? Description { get; private set; }
    
    /// <summary>
    /// 是否启用
    /// </summary>
    public bool IsEnabled { get; private set; }

    private Organization()
    {
        Name = string.Empty;
        Code = string.Empty;
    }

    public Organization(
        string name, 
        string code, 
        OrganizationType type,
        Guid? parentId = null,
        Guid? leaderId = null,
        int order = 0,
        string? description = null,
        bool isEnabled = true)
    {
        Name = name;
        Code = code;
        Type = type;
        ParentId = parentId;
        LeaderId = leaderId;
        Order = order;
        Description = description;
        IsEnabled = isEnabled;
    }

    public void Update(
        string? name = null,
        OrganizationType? type = null,
        Guid? leaderId = null,
        int? order = null,
        string? description = null,
        bool? isEnabled = null)
    {
        if (!string.IsNullOrWhiteSpace(name))
        {
            Name = name;
        }
        
        if (type.HasValue)
        {
            Type = type.Value;
        }
        
        if (leaderId.HasValue)
        {
            LeaderId = leaderId.Value;
        }
        
        if (order.HasValue)
        {
            Order = order.Value;
        }
        
        if (description != null)
        {
            Description = description;
        }
        
        if (isEnabled.HasValue)
        {
            IsEnabled = isEnabled.Value;
        }
    }

    public void Move(Guid? newParentId)
    {
        ParentId = newParentId;
    }

    public void Enable()
    {
        IsEnabled = true;
    }

    public void Disable()
    {
        IsEnabled = false;
    }
}
