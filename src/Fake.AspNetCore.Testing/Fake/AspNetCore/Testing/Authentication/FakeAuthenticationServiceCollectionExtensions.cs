using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;

namespace Fake.AspNetCore.Testing.Authentication;

public static class FakeAuthenticationServiceCollectionExtensions
{
    public static IServiceCollection AddFakeAuthentication(this IServiceCollection services)
    {
        services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = FakeAuthenticationSchemeDefaults.Scheme;
                options.DefaultChallengeScheme = FakeAuthenticationSchemeDefaults.Scheme;
                options.DefaultScheme = FakeAuthenticationSchemeDefaults.Scheme;
            })
            .AddScheme<FakeAuthenticationOptions, FakeAuthenticationHandler>(
                FakeAuthenticationSchemeDefaults.Scheme, _ => { });

        return services;
    }
}