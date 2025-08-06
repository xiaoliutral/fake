using Fake.Rbac.Domain.MenuAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fake.Rbac.Infrastructure.EntityConfigurations;

public class MenuEntityTypeConfiguration: IEntityTypeConfiguration<Menu>
{
    public void Configure(EntityTypeBuilder<Menu> builder)
    {
        builder.ToTable("menu", RbacDbContext.DefaultSchema);

        builder.HasKey(m => m.Id);

        builder.Property(m => m.Name)
            .IsRequired()
            .HasMaxLength(64);

        builder.Property(m => m.PermissionCode)
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

        builder.HasMany(m => m.Children)
            .WithOne()
            .HasForeignKey(c => c.PId);

        builder.HasIndex(m => m.Name);
        builder.HasIndex(m => m.PermissionCode);
    }
}