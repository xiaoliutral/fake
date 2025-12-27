using Fake.Rbac.Domain.MenuAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fake.Rbac.Infrastructure.EntityConfigurations;

public class MenuEntityTypeConfiguration: IEntityTypeConfiguration<Menu>
{
    public void Configure(EntityTypeBuilder<Menu> builder)
    {
        builder.ToTable("menu", FakeRbacDbContext.DefaultSchema);

        builder.HasKey(m => m.Id);

        builder.Property(m => m.Name)
            .IsRequired()
            .HasMaxLength(64);

        builder.Property(m => m.PermissionCode)
            .IsRequired()
            .HasMaxLength(64);

        builder.Property(m => m.Type)
            .IsRequired();

        builder.Property(m => m.Icon)
            .HasMaxLength(64);

        builder.Property(m => m.Route)
            .HasMaxLength(64);

        builder.Property(m => m.Component)
            .HasMaxLength(64);

        builder.Property(m => m.Order)
            .IsRequired();

        builder.Property(m => m.IsHidden)
            .IsRequired();

        builder.Property(m => m.IsCached)
            .IsRequired();

        builder.Property(m => m.Description)
            .HasMaxLength(256);

        builder.Property(m => m.PId)
            .IsRequired();

        // Don't configure the Children navigation property to avoid FK constraint
        // We'll handle the parent-child relationship manually using PId
        builder.Ignore(m => m.Children);

        builder.HasIndex(m => m.Name);
        // PermissionCode 唯一索引
        // MySQL 的唯一索引默认允许多个 NULL 值，所以不需要额外的过滤条件
        builder.HasIndex(m => m.PermissionCode).IsUnique();
        builder.HasIndex(m => m.PId); // Index for parent lookups
    }
}