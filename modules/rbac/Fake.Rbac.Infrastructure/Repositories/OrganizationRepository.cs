using Fake.DomainDrivenDesign.Repositories.EntityFrameWorkCore;
using Fake.Rbac.Domain.OrganizationAggregate;
using Microsoft.EntityFrameworkCore;

namespace Fake.Rbac.Infrastructure.Repositories;

public class OrganizationRepository : EfCoreRepository<FakeRbacDbContext, Organization>, IOrganizationRepository
{
    public async Task<Organization?> FindByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        var dbContext = await GetDbContextAsync(cancellationToken);
        return await dbContext.Organizations
            .FirstOrDefaultAsync(o => o.Code == code, cancellationToken);
    }

    public async Task<List<Organization>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var dbContext = await GetDbContextAsync(cancellationToken);
        return await dbContext.Organizations
            .OrderBy(o => o.Order)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Organization>> GetByParentIdAsync(Guid? parentId, CancellationToken cancellationToken = default)
    {
        var dbContext = await GetDbContextAsync(cancellationToken);
        return await dbContext.Organizations
            .Where(o => o.ParentId == parentId)
            .OrderBy(o => o.Order)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> HasChildrenAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var dbContext = await GetDbContextAsync(cancellationToken);
        return await dbContext.Organizations
            .AnyAsync(o => o.ParentId == id, cancellationToken);
    }
}
