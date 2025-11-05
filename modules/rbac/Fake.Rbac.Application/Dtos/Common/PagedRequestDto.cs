using System.ComponentModel.DataAnnotations;

namespace Fake.Rbac.Application.Dtos.Common;

/// <summary>
/// 分页请求 DTO
/// </summary>
public class PagedRequestDto
{
    /// <summary>
    /// 页码（从1开始）
    /// </summary>
    [Range(1, int.MaxValue)]
    public int Page { get; set; } = 1;

    /// <summary>
    /// 每页数量
    /// </summary>
    [Range(1, 100)]
    public int PageSize { get; set; } = 20;

    /// <summary>
    /// 排序字段
    /// </summary>
    public string? OrderBy { get; set; }

    /// <summary>
    /// 是否降序
    /// </summary>
    public bool Descending { get; set; } = false;
}

