namespace Fake.Domain.Dtos;

/// <summary>
/// 分页结果
/// </summary>
/// <param name="totalCount"></param>
/// <param name="items"></param>
/// <typeparam name="T"></typeparam>
public class PagedResult<T>(long totalCount, List<T> items) : ListResult<T>(items)
{
    /// <summary>
    /// 总条数
    /// </summary>
    public long TotalCount { get; set; } = totalCount;

    public PagedResult() : this(0, [])
    {
    }
}