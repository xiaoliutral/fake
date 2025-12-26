using System.Security.Claims;
using Fake.Security.Claims;

namespace Fake.Users;

public class CurrentUser(ICurrentPrincipalAccessor currentPrincipalAccessor) : ICurrentUser
{
    /*
     * see: public virtual bool IsAuthenticated => !string.IsNullOrEmpty(this.m_authenticationType);
     */
    public virtual bool IsAuthenticated => currentPrincipalAccessor.Principal?.Identity?.IsAuthenticated ?? false;
    public virtual Guid? Id => currentPrincipalAccessor.Principal?.FindUserId();

    public virtual T? GetId<T>()
    {
        var claimValue = FindClaimOrNull(FakeClaimTypes.UserId)?.Value;
        if (string.IsNullOrWhiteSpace(claimValue))
        {
            return default;
        }

        if (typeof(T) == typeof(string)) return (T?)(object)claimValue;
        
        if (typeof(T) == typeof(Guid))
            return (T?)(object)(Guid.TryParse(claimValue, out var id) ? id : Guid.Empty);
        
        if (typeof(T) == typeof(long))
            return (T?)(object)(long.TryParse(claimValue, out var id) ? id : 0);
        
        if (typeof(T) == typeof(int))
            return (T?)(object)(int.TryParse(claimValue, out var id) ? id : 0);
        
        throw new FakeException($"不支持此类型[{typeof(T).Name}]的用户id");
    }

    public Guid? TenantId => currentPrincipalAccessor.Principal?.FindTenantId();
    public virtual string? UserName => this.FindClaimValueOrNull(FakeClaimTypes.UserName);
    public string? Email => this.FindClaimValueOrNull(FakeClaimTypes.Email);
    public virtual string[] Roles => this.FindClaimValues(FakeClaimTypes.Role);

    public virtual Claim? FindClaimOrNull(string claimType)
    {
        return currentPrincipalAccessor
            .Principal?
            .Claims
            .FirstOrDefault(c => c.Type == claimType);
    }

    public Claim[] FindClaims(string claimType)
    {
        return currentPrincipalAccessor
            .Principal?
            .Claims
            .Where(c => c.Type == claimType)
            .ToArray() ?? [];
    }
}