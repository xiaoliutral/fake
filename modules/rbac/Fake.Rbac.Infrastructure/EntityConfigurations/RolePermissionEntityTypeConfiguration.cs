using Fake.Rbac.Domain.RoleAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fake.Rbac.Infrastructure.EntityConfigurations;

public class RolePermissionEntityTypeConfiguration: IEntityTypeConfiguration<RolePermission>
{
    public void Configure(EntityTypeBuilder<RolePermission> builder)
    {
        builder.ToTable("role_permission", FakeRbacDbContext.DefaultSchema);

        builder.HasIndex(rp => new { rp.RoleId, rp.PermissionCode }).IsUnique();

        builder.Property(rp => rp.RoleId).IsRequired();

        builder.Property(rp => rp.PermissionCode).IsRequired();
    }
}