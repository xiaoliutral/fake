using Domain.Aggregates.OrderAggregate;
using Fake.Domain.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace Tests;

public class RepositoryTests : AppTestBase
{
    protected readonly IRepository<Order> OrderRepository;

    public RepositoryTests()
    {
        OrderRepository = ServiceProvider.GetRequiredService<IRepository<Order>>();
    }

    [Fact]
    public async Task GetAsync()
    {
        var order = await OrderRepository.FirstOrDefaultAsync(x => x.Id == AppTestDataBuilder.OrderId);
        order.ShouldNotBeNull();
    }

    [Fact]
    public async Task GetListAsync()
    {
        var orders = await OrderRepository.GetListAsync();
        orders.Count.ShouldBeGreaterThan(0);
    }
}