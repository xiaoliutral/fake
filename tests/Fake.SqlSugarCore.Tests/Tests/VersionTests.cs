﻿using Domain.Aggregates.OrderAggregate;
using Fake.Data;
using Fake.Data.Filtering;
using Fake.Domain.Entities.Auditing;
using Fake.Domain.Repositories;
using Fake.UnitOfWork;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Tests;

public class VersionTests : AppTestBase
{
    protected readonly IRepository<Order> OrderRepository;
    protected readonly IDataFilter<ISoftDelete> SoftDeleteDataFilter;

    public VersionTests()
    {
        OrderRepository = ServiceProvider.GetRequiredService<IRepository<Order>>();
        SoftDeleteDataFilter = ServiceProvider.GetRequiredService<IDataFilter<ISoftDelete>>();
    }

    [Fact]
    public async Task 脏读不会被更新()
    {
        var order = await OrderRepository.FirstAsync(x => x.Id == AppTestDataBuilder.OrderId);
        order.SetDescription("hello");

        var order2 = await OrderRepository.FirstAsync(x => x.Id == AppTestDataBuilder.OrderId);
        order2.SetDescription("ok");
        await OrderRepository.UpdateAsync(order);
        //And updating my old entity throws exception!
        await Assert.ThrowsAsync<FakeDbConcurrencyException>(async () =>
        {
            await OrderRepository.UpdateAsync(order2);
        });
    }

    [Fact]
    public async Task 脏读更新会抛异常()
    {
        var order = await OrderRepository.FirstAsync(x => x.Id == AppTestDataBuilder.OrderId);

        var order2 = await OrderRepository.FirstAsync(x => x.Id == AppTestDataBuilder.OrderId);
        order2.SetDescription("hello2");
        await OrderRepository.UpdateAsync(order2);

        await Assert.ThrowsAsync<FakeDbConcurrencyException>(() =>
        {
            order.SetDescription("hello");
            return OrderRepository.UpdateAsync(order);
        });
    }

    [Fact]
    public async Task 脏读软删除不会被并发更新限制()
    {
        var order = await OrderRepository.FirstAsync(x => x.Id == AppTestDataBuilder.OrderId);

        var order2 = await OrderRepository.FirstAsync(x => x.Id == AppTestDataBuilder.OrderId);
        order2.SetDescription("hello2");
        await OrderRepository.UpdateAsync(order2);

        await OrderRepository.DeleteAsync(order);
    }
}