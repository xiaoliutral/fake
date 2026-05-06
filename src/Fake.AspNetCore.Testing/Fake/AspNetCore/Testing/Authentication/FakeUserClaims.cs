using System.Security.Claims;
using Fake.DependencyInjection;

namespace Fake.AspNetCore.Testing.Authentication;

public class FakeUserClaims : ISingletonDependency
{
    public List<Claim> Claims { get; } = new List<Claim>();
}
