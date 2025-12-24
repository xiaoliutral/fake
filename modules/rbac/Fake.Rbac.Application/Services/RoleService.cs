using Fake.Application;
using Fake.Domain.Exceptions;
using Fake.ObjectMapping;
using Fake.Rbac.Application.Dtos.Common;
using Fake.Rbac.Application.Dtos.Role;
using Fake.Rbac.Application.Dtos.User;
using Fake.Rbac.Domain.RoleAggregate;
using Fake.Rbac.Domain.UserAggregate;
using Fake.UnitOfWork;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Fake.Rbac.Application.Services;

[Authorize]
[ApiExplorerSettings(GroupName = "RBAC")]
public class RoleService : ApplicationService, IRoleService
{
    private readonly IRoleRepository _roleRepository;
    private readonly IUserRepository _userRepository;
    private readonly IObjectMapper _objectMapper;

    public RoleService(
        IRoleRepository roleRepository,
        IUserRepository userRepository,
        IObjectMapper objectMapper)
    {
        _roleRepository = roleRepository;
        _userRepository = userRepository;
        _objectMapper = objectMapper;
    }

    public async Task<RoleDto> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var role = await _roleRepository.GetWithPermissionsAsync(id, cancellationToken);
        if (role == null)
        {
            throw new DomainException($"角色不存在：{id}");
        }

        return _objectMapper.Map<Role, RoleDto>(role);
    }
    
    public virtual async Task<PagedResultDto<RoleDto>> GetListAsync(RolePagedRequestDto input, CancellationToken cancellationToken = default)
    {
        IQueryable<Role> query = (await _roleRepository.GetQueryableAsync(cancellationToken))
            .Include(r => r.Permissions);

        // 关键字搜索
        if (!string.IsNullOrWhiteSpace(input.Keyword))
        {
            query = query.Where(r =>
                r.Name.Contains(input.Keyword) ||
                r.Code.Contains(input.Keyword));
        }

        // 排序
        query = input.Descending
            ? query.OrderByDescending(r => r.CreateTime)
            : query.OrderBy(r => r.CreateTime);

        // 分页
        var total = await query.LongCountAsync(cancellationToken);
        var roles = await query
            .Skip((input.Page - 1) * input.PageSize)
            .Take(input.PageSize)
            .ToListAsync(cancellationToken);

        var dtos = _objectMapper.Map<List<Role>, List<RoleDto>>(roles.Cast<Role>().ToList());

        return new PagedResultDto<RoleDto>(total, dtos);
    }

    public async Task<List<RoleSimpleDto>> GetAllRolesAsync(CancellationToken cancellationToken = default)
    {
        var query = await _roleRepository.GetQueryableAsync(cancellationToken);
        var roles = await query
            .OrderBy(r => r.Name)
            .ToListAsync(cancellationToken);

        return _objectMapper.Map<List<Role>, List<RoleSimpleDto>>(roles);
    }

    public async Task<RoleDto> CreateAsync(RoleCreateDto input, CancellationToken cancellationToken = default)
    {
        // 检查编码是否已存在
        if (await _roleRepository.IsCodeExistsAsync(input.Code, cancellationToken: cancellationToken))
        {
            throw new DomainException($"角色编码已存在：{input.Code}");
        }

        var role = new Role(input.Name, input.Code);

        // 分配权限
        if (input.Permissions != null && input.Permissions.Any())
        {
            foreach (var permissionCode in input.Permissions)
            {
                role.AddPermission(permissionCode);
            }
        }

        await _roleRepository.InsertAsync(role, cancellationToken: cancellationToken);

        return await GetAsync(role.Id, cancellationToken);
    }

    public async Task<RoleDto> UpdateAsync(Guid id, RoleUpdateDto input, CancellationToken cancellationToken = default)
    {
        var role = await _roleRepository.FirstAsync(r => r.Id == id, cancellationToken: cancellationToken);

        role.Update(input.Name);

        await _roleRepository.UpdateAsync(role, cancellationToken: cancellationToken);

        return await GetAsync(id, cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        // 检查是否有用户正在使用该角色
        var userCount = await _roleRepository.GetUserCountAsync(id, cancellationToken);
        if (userCount > 0)
        {
            throw new DomainException($"该角色下还有 {userCount} 个用户，无法删除");
        }

        var role = await _roleRepository.FirstAsync(r => r.Id == id, cancellationToken: cancellationToken);
        await _roleRepository.DeleteAsync(role, cancellationToken: cancellationToken);
    }

    public async Task AssignPermissionsAsync(Guid roleId, List<string> permissionCodes, CancellationToken cancellationToken = default)
    {
        var role = await _roleRepository.GetWithPermissionsAsync(roleId, cancellationToken);
        if (role == null)
        {
            throw new DomainException($"角色不存在：{roleId}");
        }

        role.SetPermissions(permissionCodes);

        await _roleRepository.UpdateAsync(role, cancellationToken: cancellationToken);
    }

    public async Task<List<string>> GetRolePermissionsAsync(Guid roleId, CancellationToken cancellationToken = default)
    {
        var role = await _roleRepository.GetWithPermissionsAsync(roleId, cancellationToken);
        if (role == null)
        {
            throw new DomainException($"角色不存在：{roleId}");
        }

        return role.Permissions.Select(p => p.PermissionCode).ToList();
    }

    public async Task<List<UserSimpleDto>> GetRoleUsersAsync(Guid roleId, CancellationToken cancellationToken = default)
    {
        var users = await (await _userRepository.GetQueryableAsync(cancellationToken))
            .Include(u => ((User)u).Roles)
            .Where(u => ((User)u).Roles.Any(ur => ur.RoleId == roleId))
            .ToListAsync(cancellationToken);

        return _objectMapper.Map<List<User>, List<UserSimpleDto>>(users.Cast<User>().ToList());
    }

    public async Task<int> GetRoleUserCountAsync(Guid roleId, CancellationToken cancellationToken = default)
    {
        return await _roleRepository.GetUserCountAsync(roleId, cancellationToken);
    }
}

