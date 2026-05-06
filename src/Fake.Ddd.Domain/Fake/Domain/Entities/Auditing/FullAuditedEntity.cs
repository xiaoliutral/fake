namespace Fake.Domain.Entities.Auditing;

/// <summary>
/// 完全审计实体
/// </summary>
/// <typeparam name="TKey">id类型</typeparam>
[Serializable]
public abstract class FullAuditedEntity<TKey> : Entity<TKey>, IFullAuditedEntity
{
    public virtual Guid CreateUserId { get; set; }
    public virtual DateTime CreateTime { get; set; }
    public virtual Guid UpdateUserId { get; set; }
    public virtual DateTime UpdateTime { get; set; }
    public virtual bool IsDeleted { get; set; }
}

/// <summary>
/// 完全审计实体
/// </summary>
/// <typeparam name="TKey">id类型</typeparam>
/// <typeparam name="TUser">user id类型</typeparam>
[Serializable]
public abstract class FullAuditedEntity<TKey, TUser> : Entity<TKey>, IFullAuditedEntity<TUser>
    where TUser: notnull
{
    public virtual TUser CreateUserId { get; set; } = default!;
    public virtual DateTime CreateTime { get; set; }
    public virtual TUser UpdateUserId { get; set; } = default!;
    public virtual DateTime UpdateTime { get; set; }
    public virtual bool IsDeleted { get; set; }
}