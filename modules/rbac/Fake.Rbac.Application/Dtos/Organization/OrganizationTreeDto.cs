using Fake.Rbac.Domain.OrganizationAggregate;

namespace Fake.Rbac.Application.Dtos.Organization;

public class OrganizationTreeDto
{
    public Guid Id { get; set; }
    public Guid? ParentId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public OrganizationType Type { get; set; }
    public Guid? LeaderId { get; set; }
    public string? LeaderName { get; set; }
    public int Order { get; set; }
    public bool IsEnabled { get; set; }
    public List<OrganizationTreeDto> Children { get; set; } = new();
}
