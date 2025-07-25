﻿using Fake.DynamicProxy;
using Fake.Modularity;
using Fake.UnitOfWork;
using Microsoft.Extensions.DependencyInjection;

// ReSharper disable once CheckNamespace
namespace Fake.Auditing;

[DependsOn(typeof(FakeSecurityModule))]
[DependsOn(typeof(FakeUnitOfWorkModule))]
public class FakeAuditingModule : FakeModule
{
    public override void PreConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.OnRegistered(registrationContext =>
        {
            // 这里只是较粗粒度的判断，需不需要为类型生成拦截器，并非最终的拦截
            if (ShouldIntercept(registrationContext.ImplementationType))
            {
                registrationContext.Interceptors.TryAdd(typeof(AuditingInterceptor));
            }
        });
    }

    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.Configure<FakeAuditingOptions>(options =>
        {
            options.ApplicationName = context.Services.GetApplicationName();
        });

        context.Services.AddSingleton(typeof(IAuditingHelper), typeof(AuditingHelper));
        context.Services.AddSingleton(typeof(IAuditingStore), typeof(SimpleAuditingStore));

        context.Services.AddTransient<IAuditingManager, AuditingManager>();
        context.Services.AddTransient<AuditingInterceptor>();
    }

    private static bool ShouldIntercept(Type type)
    {
        if (DynamicProxyIgnoreTypes.Contains(type)) return false;

        if (AuditingHelper.IsAuditType(type)) return true;

        if (type.GetMethods().Any(m => m.IsDefined(typeof(AuditedAttribute), true))) return true;

        return false;
    }
}