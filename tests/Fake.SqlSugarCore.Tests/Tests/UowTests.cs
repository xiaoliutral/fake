using Domain.Aggregates.OrderAggregate;
using Fake.Domain.Repositories;
using Fake.UnitOfWork;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Tests;

public class UowTests : AppTestBase
{
    protected readonly IRepository<Order> OrderRepository;
    protected readonly IUnitOfWorkManager UowManager;


    public UowTests()
    {
        OrderRepository = ServiceProvider.GetRequiredService<IRepository<Order>>();
        UowManager = ServiceProvider.GetRequiredService<IUnitOfWorkManager>();
    }


    [Fact]
    public async Task ChildUow共享CurrentUow的上下文()
    {
        using var uow = UowManager.Begin();
        await OrderRepository.FirstAsync(x => x.Id == AppTestDataBuilder.OrderId);
        await OrderRepository.FirstAsync(x => x.Id == AppTestDataBuilder.OrderId);
    }
}