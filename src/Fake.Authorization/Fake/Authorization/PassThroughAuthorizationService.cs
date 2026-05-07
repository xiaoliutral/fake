namespace Fake.Authorization;

/// <summary>
/// 免授权
/// </summary>
public class PassThroughAuthorizationService : IAuthorizationService
{
    public Task<AuthorizationResult> AuthorizeAsync(ClaimsPrincipal user, object? resource,
        IEnumerable<IAuthorizationRequirement> requirements)
    {
        return Task.FromResult(AuthorizationResult.Success());
    }

    public Task<AuthorizationResult> AuthorizeAsync(ClaimsPrincipal user, object? resource, string policyName)
    {
        return Task.FromResult(AuthorizationResult.Success());
    }
}