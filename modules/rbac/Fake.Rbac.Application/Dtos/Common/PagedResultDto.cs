namespace Fake.Rbac.Application.Dtos.Common;

/// <summary>
/// 分页结果 DTO
/// </summary>
public class PagedResultDto<T>
{
    /// <summary>
    /// 总数量
    /// </summary>
    public long Total { get; set; }

    /// <summary>
    /// 数据列表
    /// </summary>
    public List<T> Items { get; set; } = new();

    public PagedResultDto()
    {
    }

    public PagedResultDto(long total, List<T> items)
    {
        Total = total;
        Items = items;
    }
}

