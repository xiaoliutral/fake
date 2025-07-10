using Domain.Aggregates.OrderAggregate;
using Fake.DependencyInjection;
using Fake.DomainDrivenDesign.Repositories.SqlSugarCore;
using Fake.SqlSugarCore.Tests;

namespace Repositories;

[ExposeServices(exposedServiceTypes: typeof(IOrderRepository))]
public class OrderRepository : SqlSugarRepository<OrderingContext, Order>, IOrderRepository, IScopedDependency
{
    public async Task<Order?> GetAsync(Guid orderId)
    {
        var query = await GetQueryableAsync();
        var order = await query.FirstAsync(x => x.Id == orderId);
        return order;
    }
}