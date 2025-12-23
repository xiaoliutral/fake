using Fake.EntityFrameworkCore;
using Fake.Rbac.Domain.MenuAggregate;
using Fake.Rbac.Domain.OrganizationAggregate;
using Fake.Rbac.Domain.RoleAggregate;
using Fake.Rbac.Domain.UserAggregate;
using Fake.Rbac.Infrastructure.EntityConfigurations;
using Microsoft.EntityFrameworkCore;

namespace Fake.Rbac.Infrastructure;

public class FakeRbacDbContext(DbContextOptions<FakeRbacDbContext> options)
    : EfCoreDbContext<FakeRbacDbContext>(options)
{
    // MySQL doesn't support schemas, so we set this to null
    public const string? DefaultSchema = null;

    public DbSet<User> Users { get; set; }
    public DbSet<Role> Roles { get; set; }
    public DbSet<Menu> Menus { get; set; }
    public DbSet<Organization> Organizations { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new UserEntityTypeConfiguration());
        modelBuilder.ApplyConfiguration(new RoleEntityTypeConfiguration());
        modelBuilder.ApplyConfiguration(new UserRoleEntityTypeConfiguration());
        modelBuilder.ApplyConfiguration(new RolePermissionEntityTypeConfiguration());
        modelBuilder.ApplyConfiguration(new MenuEntityTypeConfiguration());
        modelBuilder.ApplyConfiguration(new OrganizationEntityTypeConfiguration());
        base.OnModelCreating(modelBuilder);
    }
}