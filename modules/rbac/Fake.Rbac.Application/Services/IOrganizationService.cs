using Fake.Rbac.Application.Dtos.Organization;

namespace Fake.Rbac.Application.Services;

public interface IOrganizationService
{
    Task<OrganizationDto> GetAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<OrganizationDto>> GetListAsync(CancellationToken cancellationToken = default);
    Task<List<OrganizationTreeDto>> GetTreeAsync(CancellationToken cancellationToken = default);
    Task<OrganizationDto> CreateAsync(OrganizationCreateDto input, CancellationToken cancellationToken = default);
    Task<OrganizationDto> UpdateAsync(Guid id, OrganizationUpdateDto input, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task MoveAsync(Guid id, OrganizationMoveDto input, CancellationToken cancellationToken = default);
}
