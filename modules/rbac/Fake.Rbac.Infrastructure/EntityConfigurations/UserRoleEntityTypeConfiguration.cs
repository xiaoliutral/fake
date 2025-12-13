using Fake.Rbac.Domain.UserAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fake.Rbac.Infrastructure.EntityConfigurations;

public class UserRoleEntityTypeConfiguration: IEntityTypeConfiguration<UserRole>
{
    public void Configure(EntityTypeBuilder<UserRole> builder)
    {
        builder.ToTable("user_role", FakeRbacDbContext.DefaultSchema);

        builder.HasIndex(ur => new { ur.UserId, ur.RoleId }).IsUnique();

        builder.Property(ur => ur.UserId).IsRequired();

        builder.Property(ur => ur.RoleId).IsRequired();
    }
}