using Fake.AspNetCore.ExceptionHandling;
using Fake.AspNetCore.Testing;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Fake.AspNetCore.Tests;

public class AspNetCoreTestBase : AspNetCoreIntegrationTestWithTools<FakeAspNetCoreTestModule>
{
}