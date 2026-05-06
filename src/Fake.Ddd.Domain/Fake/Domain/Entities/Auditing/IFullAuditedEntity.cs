namespace Fake.Domain.Entities.Auditing;

public interface IFullAuditedEntity : IEntity, IHasCreateUserId, IHasUpdateUserId
    , IHasCreateTime, IHasUpdateTime, ISoftDelete;
    
    
public interface IFullAuditedEntity<out TUser> : IEntity, IHasCreateUserId<TUser>, IHasUpdateUserId<TUser>
    , IHasCreateTime, IHasUpdateTime, ISoftDelete
    where TUser: notnull;