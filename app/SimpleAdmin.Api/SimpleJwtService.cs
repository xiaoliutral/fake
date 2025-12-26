using System.Security.Claims;
using Fake.AspNetCore.Authentication;
using Fake.DependencyInjection;
using Fake.Rbac.Application.Jwt;
using Fake.Rbac.Application.Services;
using Fake.Rbac.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace SimpleAdmin.Api;

/// <summary>
/// 自定义simple-admin auth jwt service
/// </summary>
[Dependency(Replace = true)]
public class SimpleJwtService(IOptions<JwtOptions> jwtOptions, IUserService userService, FakeRbacDbContext rbacDbContext)
    : JwtService(jwtOptions, userService)
{
    public override async Task<List<Claim>> GenerateClaimsByUserIdAsync(Guid userId,
        CancellationToken cancellationToken)
    {
        var claims = await base.GenerateClaimsByUserIdAsync(userId, cancellationToken);
        var mapId = await rbacDbContext.Database.SqlQuery<int>($"select userid as value from user where id = {userId}")
            .FirstAsync(cancellationToken);
        claims.Add(new Claim("mapId", mapId.ToString()));
        return claims;
    }
}