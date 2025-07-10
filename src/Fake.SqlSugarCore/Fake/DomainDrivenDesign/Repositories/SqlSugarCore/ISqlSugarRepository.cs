using Fake.Domain.Repositories;
using Fake.SqlSugarCore;

namespace Fake.DomainDrivenDesign.Repositories.SqlSugarCore;

public interface ISqlSugarRepository<TDbContext, TEntity> : IRepository<TEntity>
    where TDbContext : SugarDbContext<TDbContext>
    where TEntity : class, IAggregateRoot, new()
{
    Task<ISqlSugarClient> GetDbContextAsync(CancellationToken cancellationToken = default);
}