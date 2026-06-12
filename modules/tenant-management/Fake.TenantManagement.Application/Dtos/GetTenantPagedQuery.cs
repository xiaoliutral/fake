using Fake.Domain.Dtos;

namespace Fake.TenantManagement.Application.Dtos;

public class GetTenantPagedQuery : PagedQuery
{
    /// <summary>
    /// 租户名称
    /// </summary>
    public string? Name { get; set; }
}