using System.Net;
using Fake.Application.Dtos;
using Fake.AspNetCore.ExceptionHandling;
using Fake.AspNetCore.Localization;
using Fake.AspNetCore.Testing.Authentication;
using Fake.Localization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace Fake.AspNetCore.Tests.ExceptionHandling;

public class DomainOrBusinessExceptionTests : AspNetCoreTestBase
{
    private readonly ITestOutputHelper _testOutputHelper;

    public DomainOrBusinessExceptionTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    protected override void ConfigureServices(HostBuilderContext context, IServiceCollection services)
    {
        services.Configure<FakeAspNetCoreMvcOptions>(options =>
        {
            options.ApplicationServices2Controller<FakeAspNetCoreTestModule>();
        });

        var partManager = services.GetInstance<ApplicationPartManager>();
        partManager.ApplicationParts.TryAdd(new AssemblyPart(typeof(FakeAspNetCoreTestModule).Assembly));

        services.AddFakeAuthentication();
    }

    protected override void ConfigureApplication(WebApplication app)
    {
        app.UseFakeRequestLocalization();

        // 异常处理
        app.UseFakeExceptionHandling();

        // 路由 - 必须在认证和授权之前
        app.UseRouting();

        // 认证和授权
        app.UseAuthentication();
        app.UseAuthorization();

        // 端点映射
        app.MapControllers();
    }

    [Fact]
    public async Task 测试授权异常()
    {
        await GetResponseAsStringAsync("/api/simple/authorization-exception", HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task 测试业务逻辑异常()
    {
        var res = await GetResponseAsync<ApplicationExceptionResponse>("throw-business-exception",
            HttpStatusCode.BadRequest);
        res.Message.ShouldBe("你好,xiaolipro");
    }

    [Fact]
    public async Task 切换多语言()
    {
        using var _ = CultureHelper.UseCulture("en");
        var res = await GetResponseAsync<ApplicationExceptionResponse>("ex", HttpStatusCode.BadRequest);
        res.Message.ShouldBe("Hello xiaolipro");
    }
}