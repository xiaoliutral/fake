using Fake.Authorization.Permissions;
using Fake.DependencyInjection;

namespace Fake.Authorization.Tests.Permissions;

public class FakePermissionStore : IPermissionStore, ITransientDependency
{
    public Task<bool> IsGrantedAsync(string permission, string ownerName, string ownerKey)
    {
        return Task.FromResult(permission.StartsWith("user"));
    }
}