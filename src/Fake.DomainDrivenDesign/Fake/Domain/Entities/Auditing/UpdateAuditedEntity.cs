namespace Fake.Domain.Entities.Auditing;

/// <summary>
/// 更新审计实体
/// </summary>
/// <typeparam name="TKey">id类型</typeparam>
[Serializable]
public abstract class UpdateAuditedEntity<TKey> : Entity<TKey>, IHasUpdateUserId, IHasUpdateTime
{
    public virtual Guid UpdateUserId { get; set; }
    public virtual DateTime UpdateTime { get; set; }
}

/// <summary>
/// 更新审计实体
/// </summary>
/// <typeparam name="TKey">id类型</typeparam>
/// <typeparam name="TUser"></typeparam>
[Serializable]
public abstract class UpdateAuditedEntity<TKey, TUser> : Entity<TKey>, IHasUpdateUserId<TUser>, IHasUpdateTime
{
    public virtual required TUser UpdateUserId { get; set; }
    public virtual DateTime UpdateTime { get; set; }
}