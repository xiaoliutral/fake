using Domain.Aggregates.OrderAggregate;
using Fake.Data.Filtering;
using Fake.Domain.Entities.Auditing;
using Fake.Domain.Repositories;
using Fake.Modularity;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace Tests;

public abstract class DataFilterTests<TStartupModule> : ApplicationTestBase<TStartupModule> where TStartupModule : IFakeModule
{
    protected readonly IDataFilter<ISoftDelete> SoftDeleteDataFilter;
    protected readonly IRepository<Order> OrderRepository;

    public DataFilterTests()
    {
        OrderRepository = ServiceProvider.GetRequiredService<IRepository<Order>>();
        SoftDeleteDataFilter = ServiceProvider.GetRequiredService<IDataFilter<ISoftDelete>>();
    }

    [Fact]
    public async Task 禁用软删过滤()
    {
        await SoftDeleteAsync();

        using (SoftDeleteDataFilter.Disable())
        {
            var order = await OrderRepository.FirstOrDefaultAsync(x => x.Id == TestDataBuilder.OrderId);
            order.ShouldNotBeNull();
        }

        var order2 = await OrderRepository.FirstOrDefaultAsync(x => x.Id == TestDataBuilder.OrderId);
        order2.ShouldBeNull();
    }

    private async Task SoftDeleteAsync()
    {
        var order = await OrderRepository.FirstOrDefaultAsync(x => x.Id == TestDataBuilder.OrderId);
        order.ShouldNotBeNull();
        order.CreateTime.ShouldBeLessThanOrEqualTo(FakeClock.Now);

        await OrderRepository.DeleteAsync(order);
        order.IsDeleted.ShouldBe(true);

        order.UpdateTime.ShouldBeLessThanOrEqualTo(FakeClock.Now);
    }
}