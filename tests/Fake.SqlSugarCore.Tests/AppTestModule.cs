using Application.DomainEventHandlers.BuyerAndPaymentMethodVerifiedEvent;
using Application.DomainEventHandlers.OrderStartedEvent;
using Domain.Events;
using Fake.Autofac;
using Fake.DomainDrivenDesign;
using Fake.EventBus;
using Fake.Modularity;
using Microsoft.Extensions.DependencyInjection;

[DependsOn(typeof(FakeAutofacModule))]
[DependsOn(typeof(FakeDomainDrivenDesignModule))]
public class FakeAppTestModule : FakeModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddSingleton(typeof(IEventHandler<OrderStartedDomainEvent>),
            typeof(ValidateOrAddBuyerAggregateWhenOrderStartedDomainEventHandler));

        context.Services.AddSingleton(typeof(IEventHandler<BuyerAndPaymentMethodVerifiedDomainEvent>),
            typeof(UpdateOrderWhenBuyerAndPaymentMethodVerifiedDomainEventHandler));

        context.Services.AddTransient<AppTestDataBuilder>();
    }

    public override void ConfigureApplication(ApplicationConfigureContext context)
    {
        context.ServiceProvider.GetRequiredService<AppTestDataBuilder>().BuildAsync().GetAwaiter().GetResult();
        //SyncContext.Run(() => context.ServiceProvider.GetRequiredService<AppTestDataBuilder>().BuildAsync());
    }
}