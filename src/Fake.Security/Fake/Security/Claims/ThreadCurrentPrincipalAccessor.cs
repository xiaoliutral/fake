using System.Security.Claims;

namespace Fake.Security.Claims;

/// <summary>
/// 基于Thread访问ClaimsPrincipal
/// </summary>
public class ThreadCurrentPrincipalAccessor : AbstractCurrentPrincipalAccessor
{
    protected override ClaimsPrincipal? GetClaimsPrincipal()
    {
        return Thread.CurrentPrincipal?.As<ClaimsPrincipal>();
    }
}