using Fake.EntityFrameworkCore;
using Fake.FileManagement.Domain.FileAggregate;
using Fake.FileManagement.Infrastructure.EntityConfigurations;
using Microsoft.EntityFrameworkCore;

namespace Fake.FileManagement.Infrastructure;

public class FileManagementDbContext(DbContextOptions<FileManagementDbContext> options)
    : EfCoreDbContext<FileManagementDbContext>(options)
{
    public DbSet<StoredFile> StoredFiles { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new StoredFileEntityTypeConfiguration());
        base.OnModelCreating(modelBuilder);
    }
}
