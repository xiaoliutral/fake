using Fake.EntityFrameworkCore.Modeling;
using Fake.Rbac.Domain;
using Fake.Rbac.Domain.UserAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fake.Rbac.Infrastructure.EntityConfigurations;

public class UserEntityTypeConfiguration: IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("user", RbacDbContext.DefaultSchema);

        builder.TryConfigureByConvention();

        builder.Property(t => t.Name).IsRequired().HasMaxLength(FakeGlobalConsts.MaxUserNameLength);

        builder.HasMany(u => u.Roles).WithMany().UsingEntity<UserRole>();

        builder.Property(t => t.Account).IsRequired().HasMaxLength(32);

        builder.Property(t => t.Password).IsRequired().HasMaxLength(32);

        builder.Property(t => t.Email).HasMaxLength(32);

        builder.Property(t => t.Avatar).HasMaxLength(32);

        builder.HasIndex(u => u.Account).IsUnique();

        builder.HasIndex(u => u.Email).IsUnique().HasFilter("[Email] IS NOT NULL");

        builder.HasIndex(u => u.Name);
    }
}