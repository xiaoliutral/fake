var builder = WebApplication.CreateSlimBuilder(args);
builder.Services.AddApplication<ConsulClientDemoModule>();
builder.Configuration.AddConsul(
    "appsettings.json",
    options =>
    {
        options.WaitTime = TimeSpan.FromSeconds(5);
        options.ReloadOnChange = true; // hot reload
    });
var app = builder.Build();
app.InitializeApplication();
app.MapGet("/", () => "Hello World!");
app.MapGet("/health", (ILogger<Program> logger) => logger.LogInformation("健康检查"));

app.Run();