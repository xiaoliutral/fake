using Fake.DomainDrivenDesign.Repositories.EntityFrameWorkCore;
using Fake.Rbac.Domain.MenuAggregate;

namespace Fake.Rbac.Infrastructure.Repositories;

public interface IEfCoreMenuRepository : IMenuRepository, IEfCoreRepository<RbacDbContext, Menu>
{
}

