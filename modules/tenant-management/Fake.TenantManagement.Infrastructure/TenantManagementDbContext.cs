using Fake.EntityFrameworkCore;
using Fake.TenantManagement.Domain.TenantAggregate;
using Fake.TenantManagement.Infrastructure.EntityConfigurations;
using Microsoft.EntityFrameworkCore;

namespace Fake.TenantManagement.Infrastructure;

public class TenantManagementDbContext(DbContextOptions<TenantManagementDbContext> options)
    : EfCoreDbContext<TenantManagementDbContext>(options)
{
    public static string? DefaultSchema => FakeDbProperties.DbSchema;

    public DbSet<Tenant> Tenants { get; set; }
    public DbSet<TenantConnectionString> TenantConnectionStrings { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new TenantEntityTypeConfiguration());
        modelBuilder.ApplyConfiguration(new TenantConnectionStringEntityTypeConfiguration());
        base.OnModelCreating(modelBuilder);
    }
}