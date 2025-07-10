namespace Fake.MultiTenant;

public interface ITenantStore
{
    Task<TenantConfiguration?> FirstOrDefaultAsync(Guid id);

    Task<TenantConfiguration?> FirstOrDefaultAsync(string code);
}