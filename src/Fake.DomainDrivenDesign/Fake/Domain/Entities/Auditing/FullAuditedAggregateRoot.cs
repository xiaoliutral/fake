namespace Fake.Domain.Entities.Auditing;

[Serializable]
public abstract class FullAuditedAggregateRoot<TKey> : AggregateRoot<TKey>, IFullAuditedEntity
{
    public virtual Guid CreateUserId { get; set; }
    public virtual DateTime CreateTime { get; set; }
    public virtual Guid UpdateUserId { get; set; }
    public virtual DateTime UpdateTime { get; set; }
    public virtual bool IsDeleted { get; set; }
}

[Serializable]
public abstract class FullAuditedAggregateRoot<TKey, TUser> : AggregateRoot<TKey>, IFullAuditedEntity<TUser>
{
    public virtual required TUser CreateUserId { get; set; }
    public virtual DateTime CreateTime { get; set; }
    public virtual required TUser UpdateUserId { get; set; }
    public virtual DateTime UpdateTime { get; set; }
    public virtual bool IsDeleted { get; set; }
}