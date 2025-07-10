using System.Security.Claims;

namespace Fake.Security.Claims;

public interface IFakeClaimsPrincipalFactory
{
    Task<ClaimsPrincipal?> CreateAsync(ClaimsPrincipal? claimsPrincipal = null);
}