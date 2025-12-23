using Fake.Application;
using Fake.Domain.Exceptions;
using Fake.ObjectMapping;
using Fake.Rbac.Application.Dtos.Organization;
using Fake.Rbac.Domain.OrganizationAggregate;
using Fake.Rbac.Domain.UserAggregate;
using Microsoft.AspNetCore.Mvc;

namespace Fake.Rbac.Application.Services;

[ApiExplorerSettings(GroupName = "RBAC")]
public class OrganizationService(
    IOrganizationRepository organizationRepository,
    IUserRepository userRepository,
    IObjectMapper objectMapper)
    : ApplicationService, IOrganizationService
{
    public async Task<OrganizationDto> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var organization = await organizationRepository.FirstAsync(o => o.Id == id, cancellationToken: cancellationToken);
        var dto = objectMapper.Map<Organization, OrganizationDto>(organization);

        // 获取负责人名称
        if (organization.LeaderId.HasValue)
        {
            var leader = await userRepository.FirstOrDefaultAsync(u => u.Id == organization.LeaderId.Value, cancellationToken: cancellationToken);
            if (leader != null)
            {
                dto.LeaderName = leader.Name;
            }
        }

        return dto;
    }

    public async Task<List<OrganizationDto>> GetListAsync(CancellationToken cancellationToken = default)
    {
        var organizations = await organizationRepository.GetAllAsync(cancellationToken);
        var dtos = new List<OrganizationDto>();

        foreach (var org in organizations)
        {
            var dto = objectMapper.Map<Organization, OrganizationDto>(org);

            // 获取负责人名称
            if (org.LeaderId.HasValue)
            {
                var leader = await userRepository.FirstOrDefaultAsync(u => u.Id == org.LeaderId.Value, cancellationToken: cancellationToken);
                if (leader != null)
                {
                    dto.LeaderName = leader.Name;
                }
            }

            dtos.Add(dto);
        }

        return dtos;
    }

    public async Task<List<OrganizationTreeDto>> GetTreeAsync(CancellationToken cancellationToken = default)
    {
        var organizations = await organizationRepository.GetAllAsync(cancellationToken);
        var dtos = objectMapper.Map<List<Organization>, List<OrganizationTreeDto>>(organizations);

        // 获取所有负责人信息
        var leaderIds = organizations.Where(o => o.LeaderId.HasValue).Select(o => o.LeaderId!.Value).Distinct().ToList();
        var leaders = await userRepository.GetListAsync(u => leaderIds.Contains(u.Id), cancellationToken: cancellationToken);
        var leaderDict = leaders.ToDictionary(u => u.Id, u => u.Name);

        // 设置负责人名称
        foreach (var dto in dtos)
        {
            if (dto.LeaderId.HasValue && leaderDict.TryGetValue(dto.LeaderId.Value, out var leaderName))
            {
                dto.LeaderName = leaderName;
            }
        }

        // 构建树形结构
        return BuildTree(dtos, null);
    }

    public async Task<OrganizationDto> CreateAsync(OrganizationCreateDto input, CancellationToken cancellationToken = default)
    {
        // 检查编码是否已存在
        var existing = await organizationRepository.FindByCodeAsync(input.Code, cancellationToken);
        if (existing != null)
        {
            throw new DomainException($"组织编码已存在：{input.Code}");
        }

        // 如果有父级，验证父级是否存在
        if (input.ParentId.HasValue)
        {
            var parent = await organizationRepository.FirstOrDefaultAsync(o => o.Id == input.ParentId.Value, cancellationToken: cancellationToken);
            if (parent == null)
            {
                throw new DomainException($"父级组织不存在：{input.ParentId}");
            }
        }

        // 如果有负责人，验证负责人是否存在
        if (input.LeaderId.HasValue)
        {
            var leader = await userRepository.FirstOrDefaultAsync(u => u.Id == input.LeaderId.Value, cancellationToken: cancellationToken);
            if (leader == null)
            {
                throw new DomainException($"负责人不存在：{input.LeaderId}");
            }
        }

        var organization = new Organization(
            input.Name,
            input.Code,
            input.Type,
            input.ParentId,
            input.LeaderId,
            input.Order,
            input.Description,
            input.IsEnabled
        );

        await organizationRepository.InsertAsync(organization, cancellationToken: cancellationToken);
        await UnitOfWorkManager.Current!.SaveChangesAsync(cancellationToken);

        return await GetAsync(organization.Id, cancellationToken);
    }

    public async Task<OrganizationDto> UpdateAsync(Guid id, OrganizationUpdateDto input, CancellationToken cancellationToken = default)
    {
        var organization = await organizationRepository.FirstAsync(o => o.Id == id, cancellationToken: cancellationToken);

        // 如果有负责人，验证负责人是否存在
        if (input.LeaderId.HasValue)
        {
            var leader = await userRepository.FirstOrDefaultAsync(u => u.Id == input.LeaderId.Value, cancellationToken: cancellationToken);
            if (leader == null)
            {
                throw new DomainException($"负责人不存在：{input.LeaderId}");
            }
        }

        organization.Update(
            input.Name,
            input.Type,
            input.LeaderId,
            input.Order,
            input.Description,
            input.IsEnabled
        );

        await organizationRepository.UpdateAsync(organization, cancellationToken: cancellationToken);
        await UnitOfWorkManager.Current!.SaveChangesAsync(cancellationToken);

        return await GetAsync(id, cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        // 检查是否有子组织
        var hasChildren = await organizationRepository.HasChildrenAsync(id, cancellationToken);
        if (hasChildren)
        {
            throw new DomainException("该组织下存在子组织，无法删除");
        }

        var organization = await organizationRepository.FirstAsync(o => o.Id == id, cancellationToken: cancellationToken);
        await organizationRepository.DeleteAsync(organization, cancellationToken: cancellationToken);
        await UnitOfWorkManager.Current!.SaveChangesAsync(cancellationToken);
    }

    public async Task MoveAsync(Guid id, OrganizationMoveDto input, CancellationToken cancellationToken = default)
    {
        var organization = await organizationRepository.FirstAsync(o => o.Id == id, cancellationToken: cancellationToken);

        // 如果有新父级，验证父级是否存在
        if (input.NewParentId.HasValue)
        {
            var parent = await organizationRepository.FirstOrDefaultAsync(o => o.Id == input.NewParentId.Value, cancellationToken: cancellationToken);
            if (parent == null)
            {
                throw new DomainException($"父级组织不存在：{input.NewParentId}");
            }

            // 不能移动到自己或自己的子组织下
            if (await IsDescendantAsync(input.NewParentId.Value, id, cancellationToken))
            {
                throw new DomainException("不能移动到自己或自己的子组织下");
            }
        }

        organization.Move(input.NewParentId);

        await organizationRepository.UpdateAsync(organization, cancellationToken: cancellationToken);
        await UnitOfWorkManager.Current!.SaveChangesAsync(cancellationToken);
    }

    private List<OrganizationTreeDto> BuildTree(List<OrganizationTreeDto> all, Guid? parentId)
    {
        return all
            .Where(o => o.ParentId == parentId)
            .Select(o =>
            {
                o.Children = BuildTree(all, o.Id);
                return o;
            })
            .OrderBy(o => o.Order)
            .ToList();
    }

    private async Task<bool> IsDescendantAsync(Guid ancestorId, Guid descendantId, CancellationToken cancellationToken)
    {
        if (ancestorId == descendantId)
        {
            return true;
        }

        var ancestor = await organizationRepository.FirstAsync(o => o.Id == ancestorId, cancellationToken: cancellationToken);
        if (ancestor.ParentId == null)
        {
            return false;
        }

        return await IsDescendantAsync(ancestor.ParentId.Value, descendantId, cancellationToken);
    }
}
