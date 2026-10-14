using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Shouldly;

namespace Fake.EventBus.Dashboard.Tests;

public class EventBusDashboardPathBaseTests
{
    [Fact]
    public async Task With_PathBase_admin_eventbus_should_return_ui()
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = Environments.Development
        });
        builder.WebHost.UseTestServer();
        builder.Services.AddRouting();
        builder.Services.AddFakeEventBusDashboard(o => o.PathMatch = "/eventbus");

        await using var app = builder.Build();
        app.UsePathBase("/admin");
        app.UseRouting();
        app.UseFakeEventBusDashboard();
        await app.StartAsync();

        var client = app.GetTestClient();
        var res = await client.GetAsync("/admin/eventbus/");
        res.StatusCode.ShouldBe(System.Net.HttpStatusCode.OK);

        var html = await res.Content.ReadAsStringAsync();
        html.ShouldContain("/admin/eventbus/api");

        var metrics = await client.GetAsync("/admin/eventbus/api/metrics");
        metrics.StatusCode.ShouldBe(System.Net.HttpStatusCode.OK);
    }
}
