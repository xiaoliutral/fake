using Fake.Rbac.Domain.OrganizationAggregate;

namespace Fake.Rbac.Application.Dtos.Organization;

public class OrganizationUpdateDto
{
    public string? Name { get; set; }
    public OrganizationType? Type { get; set; }
    public Guid? LeaderId { get; set; }
    public int? Order { get; set; }
    public string? Description { get; set; }
    public bool? IsEnabled { get; set; }
}
