using Serilog;
using SimpleAdmin.Api;

var builder = WebApplication.CreateSlimBuilder(args);
builder.Configuration.AddConsul("idwms/admin", source => source.ReloadOnChange = true);
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();

try
{
    builder.WebHost.UseUrls(builder.Configuration.GetSection("App:Urls").Get<string[]>() ?? []);
    builder.Host.UseAutofac().UseSerilog();
    builder.Services.AddApplication<SimpleAdminApiModule>();
    var app = builder.Build();
    app.InitializeApplication();
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