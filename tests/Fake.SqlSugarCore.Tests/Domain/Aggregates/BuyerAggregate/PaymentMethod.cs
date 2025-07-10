using Fake.Domain.Entities;
using Fake.Domain.Exceptions;

namespace Domain.Aggregates.BuyerAggregate;

public class PaymentMethod : Entity<Guid>
{
    public string Alias { get; set; }
    public string CardNumber { get; set; }

    public string SecurityNumber { get; set; }
    public string CardHolderName { get; set; }
    public DateTime Expiration { get; set; }
    public CardType CardType { get; set; }


    protected PaymentMethod()
    {
    }

    public PaymentMethod(CardType cardType, string alias, string cardNumber, string securityNumber,
        string cardHolderName, DateTime expiration)
    {
        CardNumber = !string.IsNullOrWhiteSpace(cardNumber)
            ? cardNumber
            : throw new DomainException(nameof(cardNumber));
        SecurityNumber = !string.IsNullOrWhiteSpace(securityNumber)
            ? securityNumber
            : throw new DomainException(nameof(securityNumber));
        CardHolderName = !string.IsNullOrWhiteSpace(cardHolderName)
            ? cardHolderName
            : throw new DomainException(nameof(cardHolderName));

        if (expiration < DateTime.UtcNow)
        {
            throw new DomainException(nameof(expiration));
        }

        Alias = alias;
        Expiration = expiration;
        CardType = cardType;
    }

    public bool IsEqualTo(CardType cardType, string cardNumber, DateTime expiration)
    {
        return CardType == cardType
               && CardNumber == cardNumber
               && Expiration == expiration;
    }
}