using Fake.Rbac.Domain.RoleAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fake.Rbac.Infrastructure.EntityConfigurations;

public class RoleEntityTypeConfiguration: IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("role", FakeRbacDbContext.DefaultSchema);

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Name)
            .IsRequired()
            .HasMaxLength(32);

        builder.Property(r => r.Code)
            .IsRequired()
            .HasMaxLength(32);
        
        builder.HasMany(r => r.Permissions)
            .WithOne()
            .HasForeignKey(rp => rp.RoleId);

        // 业务唯一由应用层保证（软删实体不加库级唯一索引）
        builder.HasIndex(r => r.Code);
    }
    
}