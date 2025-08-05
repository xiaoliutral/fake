using Fake.EntityFrameworkCore;
using Fake.Rbac.Domain.RoleAggregate;
using Fake.Rbac.Domain.UserAggregate;
using Microsoft.EntityFrameworkCore;

namespace Fake.Rbac.Infrastructure;

public class RbacDbContext(DbContextOptions<RbacDbContext> options)
    : EfCoreDbContext<RbacDbContext>(options)
{
    public const string? DefaultSchema = null;

    public DbSet<User> Users { get; set; }
    public DbSet<Role> Roles { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new UserEntityTypeConfiguration());
        modelBuilder.ApplyConfiguration(new RoleEntityTypeConfiguration());
        base.OnModelCreating(modelBuilder);
    }
}