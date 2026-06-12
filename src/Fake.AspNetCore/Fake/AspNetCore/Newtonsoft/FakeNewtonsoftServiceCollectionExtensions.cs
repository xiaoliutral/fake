using Fake.Json;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Newtonsoft.Json;

namespace Fake.AspNetCore.Newtonsoft;

public static class FakeNewtonsoftServiceCollectionExtensions
{
    public static IServiceCollection AddFakeNewtonsoft(this IServiceCollection services)
    {
        services.AddTransient<FakeDateTimeConverter>();
        services.AddTransient<FakeLongConverter>();

        // IFakeJsonSerializer
        services.Replace(ServiceDescriptor.Transient<IFakeJsonSerializer, FakeNewtonsoftJsonSerializer>());
        services.AddOptions<JsonSerializerSettings>()
            .Configure<IServiceProvider>((options, rootServiceProvider) =>
            {
                options.ContractResolver = new FakeCamelCasePropertyNamesContractResolver(
                    rootServiceProvider.GetRequiredService<FakeDateTimeConverter>());
                options.Converters.Add(rootServiceProvider.GetRequiredService<FakeLongConverter>());
            });

        // mvc
        services.AddMvcCore().AddNewtonsoftJson();
        services.AddOptions<MvcNewtonsoftJsonOptions>()
            .Configure<IServiceProvider>((options, rootServiceProvider) =>
            {
                options.SerializerSettings.ContractResolver =
                    new FakeCamelCasePropertyNamesContractResolver(
                        rootServiceProvider.GetRequiredService<FakeDateTimeConverter>());
                options.SerializerSettings.Converters.Add(rootServiceProvider.GetRequiredService<FakeLongConverter>());
            });

        return services;
    }
}