using Domain.Aggregates.OrderAggregate;
using Fake.Domain.Repositories;

namespace Application.Queries;

public interface IOrderQueries : IRootlessRepository
{
    public Task<List<OrderSummary>> GetOrderSummaryAsync(Guid userId);

    public Task<Order> AddAsync(Order order);

    public Task AddBySqlAsync(Order order);
}

public class OrderSummary
{
    public Guid ordernumber { get; set; }
    public DateTime date { get; set; }
    public OrderStatus status { get; set; }
    public double total { get; set; }
}