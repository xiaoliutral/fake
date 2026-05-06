using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Fake.AspNetCore.Testing.Authentication;

public class FakeAuthenticationHandler(
    IOptionsMonitor<FakeAuthenticationOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    FakeUserClaims fakeUserClaims)
    : AuthenticationHandler<FakeAuthenticationOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (fakeUserClaims.Claims.Count != 0)
        {
            return Task.FromResult(AuthenticateResult.Success(
                new AuthenticationTicket(
                    new ClaimsPrincipal(new ClaimsIdentity(fakeUserClaims.Claims,
                        FakeAuthenticationSchemeDefaults.Scheme)),
                    FakeAuthenticationSchemeDefaults.Scheme)));
        }

        return Task.FromResult(AuthenticateResult.NoResult());
    }
}