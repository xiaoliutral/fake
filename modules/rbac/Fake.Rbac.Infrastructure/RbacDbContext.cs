using Fake.EntityFrameworkCore;
using Fake.Rbac.Domain.MenuAggregate;
using Fake.Rbac.Domain.RoleAggregate;
using Fake.Rbac.Domain.UserAggregate;
using Fake.Rbac.Infrastructure.EntityConfigurations;
using Microsoft.EntityFrameworkCore;

namespace Fake.Rbac.Infrastructure;

public class RbacDbContext(DbContextOptions<RbacDbContext> options)
    : EfCoreDbContext<RbacDbContext>(options)
{
    public const string? DefaultSchema = null;

    public DbSet<User> Users { get; set; }
    public DbSet<Role> Roles { get; set; }
    public DbSet<Menu> Menus { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new UserEntityTypeConfiguration());
        modelBuilder.ApplyConfiguration(new RoleEntityTypeConfiguration());
        modelBuilder.ApplyConfiguration(new UserRoleEntityTypeConfiguration());
        modelBuilder.ApplyConfiguration(new RolePermissionEntityTypeConfiguration());
        modelBuilder.ApplyConfiguration(new MenuEntityTypeConfiguration());
        base.OnModelCreating(modelBuilder);
    }
}