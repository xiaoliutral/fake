using Fake.Domain.Dtos;

namespace SqlSugar;

public static class FakeSugarQueryableExtensions
{
    public static async Task<PagedResult<T>> ToPagedListAsync<T>(this ISugarQueryable<T> query,
        int pageIndex, int pageSize, CancellationToken cancellationToken = default) where T : IEntity
    {
        RefAsync<int> totalCount = 0;
        var items = await query.ToPageListAsync(pageIndex, pageSize, totalCount, cancellationToken);

        return new PagedResult<T>(totalCount, items);
    }
}