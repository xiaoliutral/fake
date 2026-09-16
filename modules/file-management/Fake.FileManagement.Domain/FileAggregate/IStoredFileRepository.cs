using Fake.Domain.Repositories;

namespace Fake.FileManagement.Domain.FileAggregate;

public interface IStoredFileRepository : IRepository<StoredFile>
{
    Task<List<StoredFile>> GetListByBizAsync(
        string bizType,
        string bizId,
        string? category = null,
        CancellationToken cancellationToken = default);
}
