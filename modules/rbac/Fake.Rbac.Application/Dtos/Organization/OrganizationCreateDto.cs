using Fake.Rbac.Domain.OrganizationAggregate;

namespace Fake.Rbac.Application.Dtos.Organization;

public class OrganizationCreateDto
{
    public Guid? ParentId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public OrganizationType Type { get; set; }
    public Guid? LeaderId { get; set; }
    public int Order { get; set; }
    public string? Description { get; set; }
    public bool IsEnabled { get; set; } = true;
}
