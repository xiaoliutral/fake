using System.ComponentModel.DataAnnotations.Schema;
using Fake.Domain.Entities;
using Fake.Domain.Exceptions;

namespace Domain.Aggregates.OrderAggregate;

[Table("OrderItems")]
public class OrderItem : Entity<Guid>
{
    public string ProductName { get; set; }
    public string? PictureUrl { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Discount { get; set; }
    public int Units { get; set; }

    public Guid OrderId { get; set; }

    public int ProductId { get; set; }

    public OrderItem()
    {
    }

    public OrderItem(int productId, string productName, decimal unitPrice, decimal discount, string pictureUrl,
        int units = 1)
    {
        if (units <= 0)
        {
            throw new DomainException("Invalid number of units");
        }

        if ((unitPrice * units) < discount)
        {
            throw new DomainException("The total of order item is lower than applied discount");
        }

        ProductId = productId;

        ProductName = productName;
        UnitPrice = unitPrice;
        Discount = discount;
        Units = units;
        PictureUrl = pictureUrl;
    }

    public string GetPictureUri() => PictureUrl;

    public string GetProductName()
    {
        return ProductName;
    }

    public decimal GetCurrentDiscount()
    {
        return Discount;
    }

    public int GetUnits()
    {
        return Units;
    }

    public decimal GetUnitPrice()
    {
        return UnitPrice;
    }

    public string GetOrderItemProductName() => ProductName;

    public void SetNewDiscount(decimal discount)
    {
        if (discount < 0)
        {
            throw new DomainException("Discount is not valid");
        }

        Discount = discount;
    }

    public void AddUnits(int units)
    {
        if (units < 0)
        {
            throw new DomainException("Invalid units");
        }

        Units += units;
    }
}