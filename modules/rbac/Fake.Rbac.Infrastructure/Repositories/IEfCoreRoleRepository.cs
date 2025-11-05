using Fake.DomainDrivenDesign.Repositories.EntityFrameWorkCore;
using Fake.Rbac.Domain.RoleAggregate;

namespace Fake.Rbac.Infrastructure.Repositories;

public interface IEfCoreRoleRepository : IRoleRepository, IEfCoreRepository<RbacDbContext, Role>
{
}

