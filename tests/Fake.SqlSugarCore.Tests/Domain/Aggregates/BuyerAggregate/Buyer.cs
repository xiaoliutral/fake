using Domain.Events;
using Fake.Auditing;
using Fake.Domain.Entities;

namespace Domain.Aggregates.BuyerAggregate;

[Audited]
public class Buyer : AggregateRoot<Guid>
{
    public Guid IdentityGuid { get; set; }

    public string Name { get; set; }

    public List<PaymentMethod> PaymentMethods { get; set; }

    public Buyer()
    {
        PaymentMethods = new List<PaymentMethod>();
    }

    public Buyer(Guid identity, string name) : this()
    {
        IdentityGuid = identity;
        Name = !name.IsNullOrWhiteSpace() ? name : throw new ArgumentNullException(nameof(name));
    }

    public PaymentMethod AddPaymentMethod(
        CardType cardType, string alias, string cardNumber,
        string securityNumber, string cardHolderName, DateTime expiration, Guid orderId)
    {
        var existingPayment = PaymentMethods
            .SingleOrDefault(p => p.IsEqualTo(cardType, cardNumber, expiration));

        if (existingPayment != null)
        {
            AddDomainEvent(new BuyerAndPaymentMethodVerifiedDomainEvent(this, existingPayment, orderId));

            return existingPayment;
        }

        var payment = new PaymentMethod(cardType, alias, cardNumber, securityNumber, cardHolderName, expiration);

        PaymentMethods.Add(payment);

        AddDomainEvent(new BuyerAndPaymentMethodVerifiedDomainEvent(this, payment, orderId));

        return payment;
    }
}