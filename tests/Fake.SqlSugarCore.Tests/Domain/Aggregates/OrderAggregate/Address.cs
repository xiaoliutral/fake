using Fake.Domain;

namespace Domain.Aggregates.OrderAggregate;

public class Address : ValueObject
{
    public String Street { get; set; }
    public String City { get; set; }
    public String State { get; set; }
    public String Country { get; set; }
    public String ZipCode { get; set; }

    public Address()
    {
    }

    public Address(string street, string city, string state, string country, string zipcode)
    {
        Street = street;
        City = city;
        State = state;
        Country = country;
        ZipCode = zipcode;
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        // Using a yield return statement to return each element one at a time
        yield return Street;
        yield return City;
        yield return State;
        yield return Country;
        yield return ZipCode;
    }
}