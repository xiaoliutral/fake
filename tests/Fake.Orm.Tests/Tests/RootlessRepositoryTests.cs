﻿using Application.Queries;
using Domain.Aggregates.BuyerAggregate;
using Domain.Aggregates.OrderAggregate;
using Fake.Modularity;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace Tests;

public abstract class RootlessRepositoryTests<TStartupModule> : ApplicationTestBase<TStartupModule>
    where TStartupModule : IFakeModule
{
    private readonly IOrderQueries _orderQueries;
    private readonly IOrderRepository _orderRepository;

    public RootlessRepositoryTests()
    {
        _orderQueries = ServiceProvider.GetRequiredService<IOrderQueries>();
        _orderRepository = ServiceProvider.GetRequiredService<IOrderRepository>();
    }

    [Fact]
    public async Task GetOrderSummaryAsync()
    {
        var orders = await _orderQueries.GetOrderSummaryAsync(TestDataBuilder.OrderId);
        orders.Count.ShouldBe(1);
        orders.First().date.ShouldBeLessThanOrEqualTo(FakeClock.Now);
        orders[0].status.ShouldBe(OrderStatus.Submitted);
        orders[0].total.ShouldBe(20.4);
    }

    [Fact]
    public async Task 无根仓储中写入会抛出异常()
    {
        var cnt = await _orderRepository.CountAsync();
        cnt.ShouldBe(1);

        var street = "fakeStreet";
        var city = "FakeCity";
        var state = "fakeState";
        var country = "fakeCountry";
        var zipcode = "FakeZipCode";
        var cardType = CardType.Amex;
        var cardNumber = "12";
        var cardSecurityNumber = "123";
        var cardHolderName = "FakeName";
        var cardExpiration = DateTime.Now.AddYears(1);
        var address = new Address(street, city, state, country, zipcode);
        var order = new Order(TestDataBuilder.UserId, "fakeName", address,
            cardType, cardNumber, cardSecurityNumber, cardHolderName, cardExpiration);

        order.SetId(Guid.NewGuid());
        Should.Throw<InvalidOperationException>(async () =>
        {
            await _orderQueries.AddAsync(order);

            cnt = await _orderRepository.CountAsync();
            cnt.ShouldBe(2);
        });
    }

    [Fact]
    public async Task 无根仓储中用sql写入会绕过检查()
    {
        var order = await _orderRepository.GetAsync(TestDataBuilder.OrderId);
        await _orderQueries.AddBySqlAsync(order);
    }
}