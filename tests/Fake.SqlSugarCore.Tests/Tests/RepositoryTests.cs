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

    [Fact]
    public async Task GetPagedListAsync_WithSorting_ShouldKeepWhere()
    {
        var total = await OrderRepository.CountAsync(x => x.Id == AppTestDataBuilder.OrderId);
        total.ShouldBe(1);

        var items = await OrderRepository.GetPagedListAsync(
            x => x.Id == AppTestDataBuilder.OrderId,
            pageIndex: 1,
            pageSize: 20,
            sorting: new Dictionary<string, bool> { ["OrderDate"] = false });

        items.Count.ShouldBe(1);
        items[0].Id.ShouldBe(AppTestDataBuilder.OrderId);
    }
}