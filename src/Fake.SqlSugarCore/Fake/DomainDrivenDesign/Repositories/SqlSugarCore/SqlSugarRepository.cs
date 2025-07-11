using System.Linq.Expressions;
using Fake.DependencyInjection;
using Fake.Domain.Entities.Auditing;
using Fake.Domain.Exceptions;
using Fake.SqlSugarCore;
using Fake.Timing;
using Fake.Users;

namespace Fake.DomainDrivenDesign.Repositories.SqlSugarCore;

public class SqlSugarRepository<TDbContext, TEntity> : ISqlSugarRepository<TDbContext, TEntity>
    where TDbContext : SugarDbContext<TDbContext>
    where TEntity : class, IAggregateRoot, new()
{
    public ILazyServiceProvider LazyServiceProvider { get; set; } = null!; // 属性注入必须public
    
    protected CancellationToken GetCancellationToken(CancellationToken cancellationToken = default) =>
        LazyServiceProvider.GetRequiredService<ICancellationTokenProvider>().FallbackToProvider(cancellationToken);
    
    protected IAuditPropertySetter AuditPropertySetter =>
        LazyServiceProvider.GetRequiredService<IAuditPropertySetter>();

    public async Task<ISqlSugarClient> GetDbContextAsync(CancellationToken cancellationToken = default)
    {
        var context = await LazyServiceProvider.GetRequiredService<ISugarDbContextProvider<TDbContext>>()
            .GetDbContextAsync(cancellationToken);

        return context.SqlSugarClient;
    }

    public virtual async Task<ISugarQueryable<TEntity>> GetQueryableAsync(CancellationToken cancellationToken = default)
    {
        var ctx = await GetDbContextAsync(cancellationToken);
        return ctx.Queryable<TEntity>();
    }

    public virtual async Task<TEntity> FirstAsync(Expression<Func<TEntity, bool>>? predicate = null, CancellationToken cancellationToken = default)
    {
        var res = await FirstOrDefaultAsync(predicate, cancellationToken);
        if (res == null) throw new EntityNotFoundException(typeof(TEntity));
        return res;
    }

    public virtual async Task<TEntity?> FirstOrDefaultAsync(Expression<Func<TEntity, bool>>? predicate = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken = GetCancellationToken(cancellationToken);

        var ctx = await GetDbContextAsync(cancellationToken);

        return await ctx.Queryable<TEntity>().FirstAsync(predicate, cancellationToken);
    }

    public virtual async Task<List<TEntity>> GetListAsync(Expression<Func<TEntity, bool>>? predicate = null,
        Dictionary<string, bool>? sorting = null, CancellationToken cancellationToken = default)
    {
        cancellationToken = GetCancellationToken(cancellationToken);
        var ctx = await GetDbContextAsync(cancellationToken);
        return await ctx.Queryable<TEntity>().WhereIF(predicate != null, predicate).ToListAsync(cancellationToken);
    }

    public virtual async Task<List<TEntity>> GetPagedListAsync(Expression<Func<TEntity, bool>>? predicate,
        int pageIndex = 1, int pageSize = 20, Dictionary<string, bool>? sorting = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken = GetCancellationToken(cancellationToken);
        var ctx = await GetDbContextAsync(cancellationToken);

        var query = ctx.Queryable<TEntity>();

        if (sorting != null)
        {
            var sortings = sorting.Select(x => new OrderByModel
            {
                FieldName = x.Key,
                OrderByType = x.Value ? OrderByType.Asc : OrderByType.Desc
            }).ToList();
            query = query.OrderBy(sortings);
        }

        return await query.ToPageListAsync(pageIndex, pageSize, GetCancellationToken(cancellationToken));
    }

    public virtual async Task<long> CountAsync(Expression<Func<TEntity, bool>>? predicate = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken = GetCancellationToken(cancellationToken);
        var ctx = await GetDbContextAsync(cancellationToken);
        return await ctx.Queryable<TEntity>()
            .WhereIF(predicate != null, predicate)
            .CountAsync(GetCancellationToken(cancellationToken));
    }

    public virtual async Task<bool> AnyAsync(Expression<Func<TEntity, bool>>? predicate = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken = GetCancellationToken(cancellationToken);
        var ctx = await GetDbContextAsync(cancellationToken);
        return await ctx.Queryable<TEntity>()
            .AnyAsync(predicate, GetCancellationToken(cancellationToken));
    }

    public virtual async Task<TEntity> InsertAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        cancellationToken = GetCancellationToken(cancellationToken);
        var ctx = await GetDbContextAsync(cancellationToken);
        return await ctx.Insertable(entity).ExecuteReturnEntityAsync();
    }

    public virtual async Task InsertRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
    {
        cancellationToken = GetCancellationToken(cancellationToken);
        var ctx = await GetDbContextAsync(cancellationToken);
        await ctx.Insertable(entities.ToArray()).ExecuteCommandAsync(cancellationToken);
    }

    public virtual async Task UpdateAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        cancellationToken = GetCancellationToken(cancellationToken);
        var ctx = await GetDbContextAsync(cancellationToken);
        try
        {
            await ctx.Updateable(entity).ExecuteCommandWithOptLockAsync(true);
        }
        catch (VersionExceptions ex)
        {
            throw new FakeDbConcurrencyException(ex);
        }
    }

    public virtual async Task UpdateRangeAsync(IEnumerable<TEntity> entities,
        CancellationToken cancellationToken = default)
    {
        cancellationToken = GetCancellationToken(cancellationToken);
        var ctx = await GetDbContextAsync(cancellationToken);
        try
        {
            await ctx.Updateable(entities.ToArray()).ExecuteCommandWithOptLockAsync(true);
        }
        catch (VersionExceptions ex)
        {
            throw new FakeDbConcurrencyException(ex);
        }
    }

    public virtual async Task DeleteAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        cancellationToken = GetCancellationToken(cancellationToken);
        var ctx = await GetDbContextAsync(cancellationToken);
        if (entity is ISoftDelete)
        {
            AuditPropertySetter.SetSoftDeleteProperty(entity);
            var update = ctx.Updateable(entity).UpdateColumns(nameof(ISoftDelete.IsDeleted));

            if (entity is IHasUpdateTime)
            {
                update.UpdateColumns(nameof(IHasUpdateTime.UpdateTime));
            }

            if (entity is IHasUpdateUserId)
            {
                update.UpdateColumns(nameof(IHasUpdateUserId.UpdateUserId));
            }

            if (entity is IHasUpdateTime or IHasUpdateUserId)
            {
                AuditPropertySetter.SetModificationProperties(entity);
            }

            await update.ExecuteCommandAsync(cancellationToken);
        }
        else
        {
            await ctx.Deleteable(entity).ExecuteCommandAsync(cancellationToken);
        }
    }

    public virtual Task DeleteRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public virtual async Task DeleteAsync(Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        cancellationToken = GetCancellationToken(cancellationToken);
        var ctx = await GetDbContextAsync(cancellationToken);
        if (typeof(TEntity).IsAssignableTo<ISoftDelete>())
        {
            var update = ctx.Updateable<TEntity>()
                .Where(predicate)
                .SetColumns(nameof(ISoftDelete.IsDeleted), true);
            if (typeof(TEntity).IsAssignableTo<IHasUpdateTime>())
            {
                var clock = LazyServiceProvider.GetRequiredService<IFakeClock>();
                update.SetColumns(nameof(IHasUpdateTime.UpdateTime), clock.Now);
            }

            if (typeof(TEntity).IsAssignableTo<IHasUpdateUserId>())
            {
                var user = LazyServiceProvider.GetRequiredService<ICurrentUser>();
                update.SetColumns(nameof(IHasUpdateUserId.UpdateUserId), user.Id);
            }

            await update.ExecuteCommandAsync(cancellationToken);
        }
        else
        {
            await ctx.Deleteable<TEntity>().Where(predicate).ExecuteCommandAsync(cancellationToken);
        }
    }
}