using Fake.DomainDrivenDesign.Repositories.EntityFrameWorkCore;
using Fake.FileManagement.Domain.FileAggregate;
using Microsoft.EntityFrameworkCore;

namespace Fake.FileManagement.Infrastructure.Repositories;

public class StoredFileRepository : EfCoreRepository<FileManagementDbContext, StoredFile>, IStoredFileRepository
{
    public async Task<List<StoredFile>> GetListByBizAsync(
        string bizType,
        string bizId,
        string? category = null,
        CancellationToken cancellationToken = default)
    {
        var dbContext = await GetDbContextAsync(cancellationToken);
        var query = dbContext.StoredFiles
            .Where(x => x.BizType == bizType && x.BizId == bizId);

        if (!string.IsNullOrWhiteSpace(category))
        {
            query = query.Where(x => x.Category == category);
        }

        return await query
            .OrderByDescending(x => x.CreateTime)
            .ToListAsync(cancellationToken);
    }
}
