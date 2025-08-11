using Fake.Helpers;
using Fake.Timing;
using Fake.Users;

namespace Fake.Domain.Entities.Auditing;

public class DefaultAuditPropertySetter(ICurrentUser currentUser, IFakeClock fakeClock) : IAuditPropertySetter
{
    public void SetCreationProperties(IEntity entity)
    {
        if (entity is IHasCreateTime entityWithCreationTime)
        {
            if (entityWithCreationTime.CreateTime == default)
            {
                ReflectionHelper.TrySetProperty(entityWithCreationTime, x => x.CreateTime, () => fakeClock.Now);
            }
        }

        if (entity is IHasCreateUserId<Guid> entityWithCreateUserId && entityWithCreateUserId.CreateUserId == Guid.Empty)
        {
            ReflectionHelper.TrySetProperty(entityWithCreateUserId, x => x.CreateUserId, () => currentUser.Id);
        }
        
        if (entity is IHasCreateUserId<long> { CreateUserId: 0 } entityWithCreateUserLongId)
        {
            ReflectionHelper.TrySetProperty(entityWithCreateUserLongId, x => x.CreateUserId, currentUser.GetId<long>);
        }
        
        if (entity is IHasCreateUserId<int> { CreateUserId: 0 } entityWithCreateUserIntId)
        {
            ReflectionHelper.TrySetProperty(entityWithCreateUserIntId, x => x.CreateUserId, currentUser.GetId<int>);
        }
    }

    public void SetModificationProperties(IEntity entity)
    {
        if (entity is IHasUpdateTime entityWithModificationTime)
        {
            ReflectionHelper.TrySetProperty(entityWithModificationTime, x => x.UpdateTime,
                () => fakeClock.Now);
        }

        if (entity is IHasUpdateUserId<Guid> entityWithUpdateUserId && entityWithUpdateUserId.UpdateUserId == Guid.Empty)
        {
            ReflectionHelper.TrySetProperty(entityWithUpdateUserId, x => x.UpdateUserId, () => currentUser.Id);
        }
        
        if (entity is IHasUpdateUserId<long> { UpdateUserId: 0 } entityWithUpdateUserLongId)
        {
            ReflectionHelper.TrySetProperty(entityWithUpdateUserLongId, x => x.UpdateUserId, currentUser.GetId<long>);
        }
        
        if (entity is IHasUpdateUserId<int> { UpdateUserId: 0 } entityWithUpdateUserIntId)
        {
            ReflectionHelper.TrySetProperty(entityWithUpdateUserIntId, x => x.UpdateUserId, currentUser.GetId<int>);
        }
    }

    public void SetSoftDeleteProperty(IEntity entity)
    {
        if (entity is not ISoftDelete entityWithSoftDelete) return;

        ReflectionHelper.TrySetProperty(entityWithSoftDelete, x => x.IsDeleted, () => true);
    }
}