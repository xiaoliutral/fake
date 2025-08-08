using Fake.Application.Dtos;

namespace Fake.TenantManagement.Application.Contracts.Dtos;

public class GetTenantPagedRequest : PagedRequest
{
    /// <summary>
    /// 租户名称
    /// </summary>
    public string? Name { get; set; }
}