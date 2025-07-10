using Domain.Aggregates.BuyerAggregate;
using Fake.DependencyInjection;
using Fake.DomainDrivenDesign.Repositories.SqlSugarCore;
using Fake.SqlSugarCore.Tests;

namespace Repositories;

public class BuyerRepository : SqlSugarRepository<OrderingContext, Buyer>, IBuyerRepository, IScopedDependency
{
    public Buyer Add(Buyer buyer)
    {
        throw new NotImplementedException();
    }

    public Buyer Update(Buyer buyer)
    {
        throw new NotImplementedException();
    }

    public Task<Buyer> FindAsync(Guid buyerIdentityGuid)
    {
        throw new NotImplementedException();
    }

    public Task<Buyer> FindByIdAsync(string id)
    {
        throw new NotImplementedException();
    }
}