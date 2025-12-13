using Fake.DomainDrivenDesign.Repositories.EntityFrameWorkCore;
using Fake.EntityFrameworkCore;
using Fake.Rbac.Domain.RoleAggregate;
using Microsoft.EntityFrameworkCore;

namespace Fake.Rbac.Infrastructure.Repositories;

public class RoleRepository : EfCoreRepository<FakeRbacDbContext, Role>, IRoleRepository
{
    public async Task<Role?> FindByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        var dbContext = await GetDbContextAsync(cancellationToken);
        return await dbContext.Roles
            .Include(r => r.Permissions)
            .FirstOrDefaultAsync(r => r.Code == code, cancellationToken);
    }

    public async Task<Role?> GetWithPermissionsAsync(Guid roleId, CancellationToken cancellationToken = default)
    {
        var dbContext = await GetDbContextAsync(cancellationToken);
        return await dbContext.Roles
            .Include(r => r.Permissions)
            .FirstOrDefaultAsync(r => r.Id == roleId, cancellationToken);
    }

    public async Task<List<Role>> GetRolesWithPermissionsAsync(List<Guid> roleIds, CancellationToken cancellationToken = default)
    {
        var dbContext = await GetDbContextAsync(cancellationToken);
        return await dbContext.Roles
            .Include(r => r.Permissions)
            .Where(r => roleIds.Contains(r.Id))
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> IsCodeExistsAsync(string code, Guid? excludeRoleId = null, CancellationToken cancellationToken = default)
    {
        var dbContext = await GetDbContextAsync(cancellationToken);
        var query = dbContext.Roles.Where(r => r.Code == code);
        
        if (excludeRoleId.HasValue)
        {
            query = query.Where(r => r.Id != excludeRoleId.Value);
        }
        
        return await query.AnyAsync(cancellationToken);
    }

    public async Task<int> GetUserCountAsync(Guid roleId, CancellationToken cancellationToken = default)
    {
        var dbContext = await GetDbContextAsync(cancellationToken);
        return await dbContext.Set<Domain.UserAggregate.UserRole>()
            .Where(ur => ur.RoleId == roleId)
            .CountAsync(cancellationToken);
    }
}

