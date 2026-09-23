using Fake.EntityFrameworkCore.Modeling;
using Fake.FileManagement.Domain.FileAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fake.FileManagement.Infrastructure.EntityConfigurations;

public class StoredFileEntityTypeConfiguration : IEntityTypeConfiguration<StoredFile>
{
    public void Configure(EntityTypeBuilder<StoredFile> builder)
    {
        builder.ToTable("stored_file");

        builder.TryConfigureByConvention();

        builder.Property(x => x.FileName).IsRequired().HasMaxLength(StoredFile.MaxFileNameLength);
        builder.Property(x => x.ContentType).HasMaxLength(StoredFile.MaxContentTypeLength);
        builder.Property(x => x.StorageSource).IsRequired().HasMaxLength(StoredFile.MaxStorageSourceLength);
        builder.Property(x => x.Size).IsRequired();
        builder.Property(x => x.Status).IsRequired();

        builder.HasIndex(x => x.StorageSource);
        builder.HasIndex(x => x.Status);
    }
}
