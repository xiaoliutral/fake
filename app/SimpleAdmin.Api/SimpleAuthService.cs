using System.Security.Claims;
using Fake.ObjectMapping;
using Fake.Rbac.Application.Dtos.Auth;
using Fake.Rbac.Application.Services;
using Fake.Rbac.Application.Services.Auth;
using Fake.Rbac.Domain.Managers;
using Fake.Rbac.Domain.UserAggregate;
using Fake.Users;

namespace SimpleAdmin.Api;

public class SimpleAuthService(AccountManager accountManager, IUserService userService, IMenuService menuService, IObjectMapper objectMapper, IJwtService jwtService, ICurrentUser currentUser, IUserRepository userRepository, IWebHostEnvironment webHostEnvironment) : AuthService(accountManager, userService, menuService, objectMapper, jwtService, currentUser, userRepository, webHostEnvironment)
{
    protected override Task<List<Claim>> GenerateClaimsByUserId(Guid userId, CancellationToken cancellationToken)
    {
        return base.GenerateClaimsByUserId(userId, cancellationToken);
    }
}