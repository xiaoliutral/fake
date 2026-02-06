using Fake.FeiShu;
using Serilog;
using SimpleAdmin.Api;

var builder = WebApplication.CreateSlimBuilder(args);
builder.Configuration.AddConsul("idwms/common");
builder.Configuration.AddConsul("idwms/admin", source => source.ReloadOnChange = true);

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();

// 添加飞书日志提供程序
builder.Logging.AddFeiShu(config =>
{
    config.IsEnabled = true;
    config.MinimumLevel = LogLevel.Information; // ILogger 最低级别
});


builder.WebHost.UseUrls(builder.Configuration.GetSection("App:Urls").Get<string[]>() ?? []);
builder.Host
    .UseAutofac()
    .UseSerilog((context, configuration) =>
        configuration.ReadFrom.Configuration(context.Configuration)
    );
builder.Services.AddApplication<SimpleAdminApiModule>();
var app = builder.Build();
app.InitializeApplication();

try
{
    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "G");
}
finally
{
    Log.CloseAndFlush();
}