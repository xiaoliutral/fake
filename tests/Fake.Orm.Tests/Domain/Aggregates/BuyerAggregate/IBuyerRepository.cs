using Fake.Domain.Repositories;

namespace Domain.Aggregates.BuyerAggregate;
//This is just the RepositoryContracts or Interface defined at the Domain Layer
//as requisite for the Buyer Aggregate

public interface IBuyerRepository : IRepository<Buyer>;