using Fake.DomainDrivenDesign.Repositories.EntityFrameWorkCore;
using Fake.EntityFrameworkCore;
using Fake.Rbac.Domain.UserAggregate;
using Microsoft.EntityFrameworkCore;

namespace Fake.Rbac.Infrastructure.Repositories;

public class UserRepository : EfCoreRepository<RbacDbContext, User>, IEfCoreUserRepository
{
    public async Task<User?> FindByAccountAsync(string account, CancellationToken cancellationToken = default)
    {
        var dbContext = await GetDbContextAsync(cancellationToken);
        return await dbContext.Users
            .Include(u => u.Roles)
            .FirstOrDefaultAsync(u => u.Account == account, cancellationToken);
    }

    public async Task<User?> GetWithRolesAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var dbContext = await GetDbContextAsync(cancellationToken);
        return await dbContext.Users
            .Include(u => u.Roles)
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
    }

    public async Task<List<User>> GetUsersWithRolesAsync(List<Guid> userIds, CancellationToken cancellationToken = default)
    {
        var dbContext = await GetDbContextAsync(cancellationToken);
        return await dbContext.Users
            .Include(u => u.Roles)
            .Where(u => userIds.Contains(u.Id))
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> IsAccountExistsAsync(string account, Guid? excludeUserId = null, CancellationToken cancellationToken = default)
    {
        var dbContext = await GetDbContextAsync(cancellationToken);
        var query = dbContext.Users.Where(u => u.Account == account);
        
        if (excludeUserId.HasValue)
        {
            query = query.Where(u => u.Id != excludeUserId.Value);
        }
        
        return await query.AnyAsync(cancellationToken);
    }
}

