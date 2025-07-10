using System.ComponentModel.DataAnnotations.Schema;
using Domain.Aggregates.BuyerAggregate;
using Domain.Events;
using Fake.Auditing;
using Fake.Domain.Entities.Auditing;
using Fake.Domain.Exceptions;
using SqlSugar;

namespace Domain.Aggregates.OrderAggregate;

[Table("Orders")]
[Audited]
public class Order : FullAuditedAggregateRoot<Guid>
{
    public DateTime OrderDate { get; set; }

    [SugarColumn(IsOwnsOne = true)] public Address Address { get; set; }

    public Guid? BuyerId { get; set; }

    public OrderStatus OrderStatus { get; set; }

    public string? Description { get; set; }


    // Draft orders have this set to true. Currently we don't check anywhere the draft status of an Order, but we could do it if needed
    public bool IsDraft { get; set; }

    [Navigate(NavigateType.OneToMany, nameof(OrderItem.OrderId))] //BookA表中的studenId
    public List<OrderItem> OrderItems { get; set; }

    public Guid? PaymentMethodId { get; set; }

    public static Order NewDraft()
    {
        var order = new Order();
        order.IsDraft = true;
        return order;
    }

    public Order()
    {
        OrderItems = new List<OrderItem>();
        IsDraft = false;
    }

    public Order(Guid userId, string userName, Address address, CardType cardType, string cardNumber,
        string cardSecurityNumber,
        string cardHolderName, DateTime cardExpiration, Guid? buyerId = null, Guid? paymentMethodId = null) : this()
    {
        BuyerId = buyerId;
        PaymentMethodId = paymentMethodId;
        OrderStatus = OrderStatus.Submitted;
        OrderDate = DateTime.UtcNow;
        Address = address;

        // Add the OrderStarterDomainEvent to the domain events collection 
        // to be raised/dispatched when comitting changes into the Database [ After DbContext.SaveChanges() ]
        var orderStartedDomainEvent = new OrderStartedDomainEvent(this, userId, userName, cardType,
            cardNumber, cardSecurityNumber,
            cardHolderName, cardExpiration);

        this.AddDomainEvent(orderStartedDomainEvent);
    }

    // DDD Patterns comment
    // This Order AggregateRoot's method "AddOrderitem()" should be the only way to add Items to the Order,
    // so any behavior (discounts, etc.) and validations are controlled by the AggregateRoot 
    // in order to maintain consistency between the whole Aggregate. 
    public void AddOrderItem(int productId, string productName, decimal unitPrice, decimal discount, string pictureUrl,
        int units = 1)
    {
        var existingOrderForProduct = OrderItems.SingleOrDefault(o => o.ProductId == productId);

        if (existingOrderForProduct != null)
        {
            //if previous line exist modify it with higher discount  and units..

            if (discount > existingOrderForProduct.GetCurrentDiscount())
            {
                existingOrderForProduct.SetNewDiscount(discount);
            }

            existingOrderForProduct.AddUnits(units);
        }
        else
        {
            //add validated new order item

            var orderItem = new OrderItem(productId, productName, unitPrice, discount, pictureUrl, units);
            OrderItems.Add(orderItem);
        }
    }

    public void SetPaymentId(Guid id)
    {
        PaymentMethodId = id;
    }

    public void SetBuyerId(Guid id)
    {
        BuyerId = id;
    }

    public void SetAwaitingValidationStatus()
    {
        if (OrderStatus == OrderStatus.Submitted)
        {
            AddDomainEvent(new OrderStatusChangedToAwaitingValidationDomainEvent(Id, OrderItems));
            OrderStatus = OrderStatus.AwaitingValidation;
        }
    }

    public void SetStockConfirmedStatus()
    {
        if (OrderStatus == OrderStatus.AwaitingValidation)
        {
            AddDomainEvent(new OrderStatusChangedToStockConfirmedDomainEvent(Id));

            OrderStatus = OrderStatus.StockConfirmed;
            Description = "All the items were confirmed with available stock.";
        }
    }

    public void SetPaidStatus()
    {
        if (OrderStatus == OrderStatus.StockConfirmed)
        {
            AddDomainEvent(new OrderStatusChangedToPaidDomainEvent(Id, OrderItems));

            OrderStatus = OrderStatus.Paid;
            Description =
                "The payment was performed at a simulated \"American Bank checking bank account ending on XX35071\"";
        }
    }

    public void SetShippedStatus()
    {
        if (OrderStatus != OrderStatus.Paid)
        {
            StatusChangeException(OrderStatus.Shipped);
        }

        OrderStatus = OrderStatus.Shipped;
        Description = "The order was shipped.";
        AddDomainEvent(new OrderShippedDomainEvent(this));
    }

    public void SetCancelledStatus()
    {
        if (OrderStatus == OrderStatus.Paid ||
            OrderStatus == OrderStatus.Shipped)
        {
            StatusChangeException(OrderStatus.Cancelled);
        }

        OrderStatus = OrderStatus.Cancelled;
        Description = $"The order was cancelled.";
        AddDomainEvent(new OrderCancelledDomainEvent(this));
    }

    public void SetCancelledStatusWhenStockIsRejected(IEnumerable<int> orderStockRejectedItems)
    {
        if (OrderStatus == OrderStatus.AwaitingValidation)
        {
            OrderStatus = OrderStatus.Cancelled;

            var itemsStockRejectedProductNames = OrderItems
                .Where(c => orderStockRejectedItems.Contains(c.ProductId))
                .Select(c => c.GetOrderItemProductName());

            var itemsStockRejectedDescription = string.Join(", ", itemsStockRejectedProductNames);
            Description = $"The product items don't have stock: ({itemsStockRejectedDescription}).";
        }
    }

    private void StatusChangeException(OrderStatus orderStatusToChange)
    {
        throw new DomainException(
            $"Is not possible to change the order status from {OrderStatus} to {orderStatusToChange}.");
    }

    public decimal GetTotal()
    {
        return OrderItems.Sum(o => o.GetUnits() * o.GetUnitPrice());
    }

    public void SetDescription(string description)
    {
        Description = description;
    }
}