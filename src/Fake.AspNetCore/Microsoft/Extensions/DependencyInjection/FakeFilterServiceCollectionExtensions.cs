using Fake.AspNetCore;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection;

public static class FakeFilterServiceCollectionExtensions
{
    public static IServiceCollection AddFakeFilter<TFilter>(this IServiceCollection services,
        ServiceLifetime lifetime = ServiceLifetime.Transient) where TFilter : class, IFakeFilter
    {
        services.TryAdd(new ServiceDescriptor(typeof(TFilter), typeof(TFilter), lifetime));
        services.Configure<MvcOptions>(options => { options.Filters.AddService<TFilter>(); });
        return services;
    }
}