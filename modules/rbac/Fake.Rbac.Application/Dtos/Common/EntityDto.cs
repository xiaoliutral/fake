namespace Fake.Rbac.Application.Dtos.Common;

/// <summary>
/// 实体 DTO 基类
/// </summary>
public class EntityDto<TKey>
{
    public TKey Id { get; set; } = default!;
}

/// <summary>
/// 审计实体 DTO 基类
/// </summary>
public class AuditedEntityDto<TKey> : EntityDto<TKey>
{
    public DateTime CreatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }
}

