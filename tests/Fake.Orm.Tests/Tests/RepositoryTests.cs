﻿using Domain.Aggregates.OrderAggregate;
using Fake.Domain.Repositories;
using Fake.Modularity;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace Tests;

public abstract class RepositoryTests<TStartupModule> : ApplicationTestBase<TStartupModule> where TStartupModule : IFakeModule
{
    protected readonly IRepository<Order> OrderRepository;

    protected RepositoryTests()
    {
        OrderRepository = ServiceProvider.GetRequiredService<IRepository<Order>>();
    }

    [Fact]
    public async Task GetAsync()
    {
        var order = await OrderRepository.FirstOrDefaultAsync(x => x.Id == TestDataBuilder.OrderId);
        order.ShouldNotBeNull();
    }

    [Fact]
    public async Task GetListAsync()
    {
        var orders = await OrderRepository.GetListAsync();
        orders.Count.ShouldBeGreaterThan(0);
    }
}