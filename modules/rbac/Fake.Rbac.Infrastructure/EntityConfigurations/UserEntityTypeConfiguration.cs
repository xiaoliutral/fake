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

        builder.Property(t => t.Avatar).HasMaxLength(128);
        
        builder.Property(t => t.OrganizationId);
        
        builder.HasMany(u => u.Roles).WithOne().HasForeignKey(ur => ur.UserId);

        // 业务唯一由应用层保证（软删实体不加库级唯一索引）
        builder.HasIndex(u => u.Account);
        builder.HasIndex(u => u.Email);
        builder.HasIndex(u => u.Name);
        builder.HasIndex(u => u.OrganizationId);
    }
}
