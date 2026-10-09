using Fake.Rbac.Domain.OrganizationAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fake.Rbac.Infrastructure.EntityConfigurations;

public class OrganizationEntityTypeConfiguration : IEntityTypeConfiguration<Organization>
{
    public void Configure(EntityTypeBuilder<Organization> builder)
    {
        builder.ToTable("organization", FakeRbacDbContext.DefaultSchema);

        builder.HasKey(o => o.Id);

        builder.Property(o => o.Name)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(o => o.Code)
            .IsRequired()
            .HasMaxLength(64);

        // 业务唯一由应用层保证（软删实体不加库级唯一索引）
        builder.HasIndex(o => o.Code);

        builder.Property(o => o.Type)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(32);

        builder.Property(o => o.Description)
            .HasMaxLength(512);

        builder.Property(o => o.ParentId);
        builder.Property(o => o.LeaderId);
        builder.Property(o => o.Order);
        builder.Property(o => o.IsEnabled);

        // 审计字段
        builder.Property(o => o.CreateUserId);
        builder.Property(o => o.CreateTime);
        builder.Property(o => o.UpdateUserId);
        builder.Property(o => o.UpdateTime);
        builder.Property(o => o.IsDeleted);
    }
}
