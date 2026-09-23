using Fake.DomainDrivenDesign.Repositories.EntityFrameWorkCore;
using Fake.FileManagement.Domain.FileAggregate;

namespace Fake.FileManagement.Infrastructure.Repositories;

public class StoredFileRepository : EfCoreRepository<FileManagementDbContext, StoredFile>, IStoredFileRepository
{
}
