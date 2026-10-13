using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Fake.EventBus.Dashboard.Tests;

public sealed class DashboardHostFixture : IAsyncLifetime
{
    private WebApplication _app = null!;
    public HttpClient Client { get; private set; } = null!;
    public IServiceProvider Services => _app.Services;

    public async Task InitializeAsync()
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = Environments.Development
        });
        builder.WebHost.UseTestServer();

        builder.Services.AddRouting();
        builder.Services.AddFakeEventBusDashboard(o =>
        {
            o.PathMatch = "/eventbus";
            o.MetricsPollIntervalMs = 1000;
        });

        _app = builder.Build();
        await _app.StartAsync();
        Client = _app.GetTestClient();
    }

    public async Task DisposeAsync()
    {
        await _app.StopAsync();
        await _app.DisposeAsync();
        Client.Dispose();
    }
}
