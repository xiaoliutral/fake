using Fake.Domain.Entities.Auditing;

namespace Fake.Rbac.Domain.UserAggregate;

public class UserRole: CreateAuditedEntity<Guid>
{
    public Guid UserId { get; private set; }

    public Guid RoleId { get; private set; }

    public UserRole()
    {
        
    }

    public UserRole(Guid userId, Guid roleId)
    {
        UserId = userId;
        RoleId = roleId;
    }
}