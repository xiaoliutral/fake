namespace Fake.Domain.Entities.Auditing;

public interface IEntityAuditPropertySetter
{
    void SetCreationProperties(IEntity entity);

    void SetModificationProperties(IEntity entity);

    void SetSoftDeleteProperty(IEntity entity);
}