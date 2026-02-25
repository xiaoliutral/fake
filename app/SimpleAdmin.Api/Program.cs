using Serilog;
using SimpleAdmin.Api;

var builder = WebApplication.CreateSlimBuilder(args);
builder.Configuration.AddConsul("idwms/common", source => source.ReloadOnChange = true);
builder.Configuration.AddConsul("idwms/admin", source => source.ReloadOnChange = true);

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();

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