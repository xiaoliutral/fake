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

        builder.Property(x => x.ObjectKey).IsRequired().HasMaxLength(StoredFile.MaxObjectKeyLength);
        builder.Property(x => x.FileName).IsRequired().HasMaxLength(StoredFile.MaxFileNameLength);
        builder.Property(x => x.ContentType).HasMaxLength(StoredFile.MaxContentTypeLength);
        builder.Property(x => x.Category).IsRequired().HasMaxLength(StoredFile.MaxCategoryLength);
        builder.Property(x => x.BizType).HasMaxLength(StoredFile.MaxBizTypeLength);
        builder.Property(x => x.BizId).HasMaxLength(StoredFile.MaxBizIdLength);
        builder.Property(x => x.Size).IsRequired();

        builder.HasIndex(x => x.ObjectKey);
        builder.HasIndex(x => new { x.BizType, x.BizId, x.Category });
        builder.HasIndex(x => x.Category);
    }
}
