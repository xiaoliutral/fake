using System.Security.Claims;

namespace Fake.Security.Claims;

public class FakeClaimsPrincipalContributorContext
{
    public ClaimsPrincipal? ClaimsPrincipal { get; }
    public IServiceProvider ServiceProvider { get; }

    public FakeClaimsPrincipalContributorContext(ClaimsPrincipal? claimsPrincipal, IServiceProvider serviceProvider)
    {
        ClaimsPrincipal = claimsPrincipal;
        ServiceProvider = serviceProvider;
    }
}