using Fake.Authorization.Permissions;
using Fake.DependencyInjection;

namespace Fake.Authorization.Tests.Permissions;

public class FakePermissionDefiner : IPermissionDefiner, ITransientDependency
{
    public Task<List<PermissionDto>> DefineAsync()
    {
        var res = new List<PermissionDto>();
        var userPermission = new PermissionDto("user");
        userPermission.AddChild("create");
        userPermission.AddChild("delete");
        res.Add(userPermission);
        res.Add(new PermissionDto("system"));
        return Task.FromResult(res);
    }
}