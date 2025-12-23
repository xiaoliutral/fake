using Fake.Domain.Repositories;

namespace Fake.Rbac.Domain.OrganizationAggregate;

public interface IOrganizationRepository : IRepository<Organization>
{
    Task<Organization?> FindByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<List<Organization>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<List<Organization>> GetByParentIdAsync(Guid? parentId, CancellationToken cancellationToken = default);
    Task<bool> HasChildrenAsync(Guid id, CancellationToken cancellationToken = default);
}
