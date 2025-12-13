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
        builder.ToTable("user", FakeRbacDbContext.DefaultSchema);

        builder.TryConfigureByConvention();

        builder.Property(t => t.Name).IsRequired().HasMaxLength(FakeGlobalConsts.MaxUserNameLength);
        
        builder.Property(t => t.Account).IsRequired().HasMaxLength(32);

        builder.OwnsOne(t => t.EncryptPassword);

        builder.Property(t => t.Email).HasMaxLength(32);

        builder.Property(t => t.Avatar).HasMaxLength(64);
        
        builder.HasMany(u => u.Roles).WithOne().HasForeignKey(ur => ur.UserId);

        builder.HasIndex(u => u.Account).IsUnique();

        builder.HasIndex(u => u.Email).IsUnique();

        builder.HasIndex(u => u.Name);
    }
}