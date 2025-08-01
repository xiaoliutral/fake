﻿using Domain.Aggregates.BuyerAggregate;
using Domain.Events;
using Fake.Auditing;
using Fake.EventBus;
using Microsoft.Extensions.Logging;

namespace Application.DomainEventHandlers.OrderStartedEvent;

public class ValidateOrAddBuyerAggregateWhenOrderStartedDomainEventHandler(
    ILogger<ValidateOrAddBuyerAggregateWhenOrderStartedDomainEventHandler> logger,
    IBuyerRepository buyerRepository)
    : IEventHandler<OrderStartedDomainEvent>
{
    public int Order { get; set; }

    [Audited]
    public async Task HandleAsync(OrderStartedDomainEvent orderStartedEvent,
        CancellationToken cancellationToken)
    {
        var buyer = await buyerRepository.FirstOrDefaultAsync(x => x.IdentityGuid == orderStartedEvent.UserId,
            cancellationToken: cancellationToken);
        bool buyerOriginallyExisted = buyer != null;

        if (!buyerOriginallyExisted)
        {
            buyer = new Buyer(orderStartedEvent.UserId, orderStartedEvent.UserName);
        }

        buyer.AddPaymentMethod(orderStartedEvent.CardType,
            $"Payment Method on {DateTime.UtcNow}",
            orderStartedEvent.CardNumber,
            orderStartedEvent.CardSecurityNumber,
            orderStartedEvent.CardHolderName,
            orderStartedEvent.CardExpiration,
            orderStartedEvent.Order.Id);

        if (buyerOriginallyExisted)
        {
            await buyerRepository.UpdateAsync(buyer, cancellationToken: cancellationToken);
        }
        else
        {
            await buyerRepository.InsertAsync(buyer, cancellationToken: cancellationToken);
        }

        logger.LogDebug("Buyer {BuyerId} and related payment method were validated or updated for orderId: {OrderId}",
            buyer.Id, orderStartedEvent.Order.Id);
    }
}