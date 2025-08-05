using Fake.Domain.Repositories;

namespace Fake.Rbac.Domain.UserAggregate;

public interface IUserRepository: IRepository<User>
{
    Task AssignRole(Guid userId, Guid roleId);
    
    Task RemoveRole(Guid userId, Guid roleId);
    
    Task AssignPermission(Guid userId, Guid permissionCode);
    
    Task RemovePermission(Guid userId, Guid permissionCode);
}