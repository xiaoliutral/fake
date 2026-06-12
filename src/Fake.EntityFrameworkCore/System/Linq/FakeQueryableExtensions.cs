using Fake.Domain.Dtos;

namespace System.Linq;

public static class FakeQueryableExtensions
{
    public static async Task<PagedResult<T>> ToPagedListAsync<T>(this IQueryable<T> queryable, int pageIndex,
        int pageSize, CancellationToken cancellationToken = default) where T : IEntity
    {
        var skip = ((pageIndex < 1 ? 1 : pageIndex) - 1) * pageSize;
        var pagedList = await queryable.Skip(skip)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
        var totalCount = await queryable.CountAsync(cancellationToken);

        return new PagedResult<T>(totalCount, pagedList);
    }
}