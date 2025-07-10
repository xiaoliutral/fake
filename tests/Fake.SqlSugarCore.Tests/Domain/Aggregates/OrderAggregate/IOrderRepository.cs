using Fake.DomainDrivenDesign.Repositories.SqlSugarCore;
using Fake.SqlSugarCore.Tests;

namespace Domain.Aggregates.OrderAggregate;

public interface IOrderRepository : ISqlSugarRepository<OrderingContext, Order>
{
    Task<Order?> GetAsync(Guid orderId);
}