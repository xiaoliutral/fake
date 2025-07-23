using System.Net;
using Fake.Application.Dtos;
using Fake.Localization;
using Microsoft.AspNetCore.Builder;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace Fake.AspNetCore.Tests.ExceptionHandling;

public class DomainOrBusinessExceptionTests: AspNetCoreTestBase
{
    private readonly ITestOutputHelper _testOutputHelper;
    private SimpleService _simpleService;

    public DomainOrBusinessExceptionTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
        _simpleService = GetRequiredService<SimpleService>();
    }

    protected override void ConfigureApplication(WebApplication app)
    {
        base.ConfigureApplication(app);

        app.UseFakeRequestLocalization();
        app.UseFakeExceptionHandling();

        app.MapGet("auth-exception", () => _simpleService.AuthorizationException());
        app.MapGet("throw-business-exception", () => _simpleService.ThrowBusinessException());
    }

    [Fact]
    public async Task 测试授权异常()
    {
        var res = await GetResponseAsStringAsync("auth-exception", HttpStatusCode.InternalServerError);
        _testOutputHelper.WriteLine(res);
    }

    [Fact]
    public async Task 测试业务逻辑异常()
    {
        var res = await GetResponseAsync<ApplicationExceptionResult>("throw-business-exception", HttpStatusCode.BadRequest);
        res.Message.ShouldBe("你好");
    }
    
    [Fact]
    public async Task 切换多语言()
    {
        using var _ = CultureHelper.UseCulture("en");
        var res = await GetResponseAsync<ApplicationExceptionResult>("throw-business-exception", HttpStatusCode.BadRequest);
        res.Message.ShouldBe("Hello xiaolipro");
    }
}