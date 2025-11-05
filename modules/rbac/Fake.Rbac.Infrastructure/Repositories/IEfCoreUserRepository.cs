using Fake.DomainDrivenDesign.Repositories.EntityFrameWorkCore;
using Fake.Rbac.Domain.UserAggregate;

namespace Fake.Rbac.Infrastructure.Repositories;

public interface IEfCoreUserRepository : IUserRepository, IEfCoreRepository<RbacDbContext, User>
{
}

