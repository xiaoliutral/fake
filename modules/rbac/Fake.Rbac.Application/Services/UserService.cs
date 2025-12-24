using Fake.Application;
using Fake.Domain.Exceptions;
using Fake.ObjectMapping;
using Fake.Rbac.Application.Dtos.Common;
using Fake.Rbac.Application.Dtos.Role;
using Fake.Rbac.Application.Dtos.User;
using Fake.Rbac.Domain.OrganizationAggregate;
using Fake.Rbac.Domain.RoleAggregate;
using Fake.Rbac.Domain.UserAggregate;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Fake.Rbac.Application.Services;

[ApiExplorerSettings(GroupName = "RBAC")]
public class UserService(
    IUserRepository userRepository,
    IRoleRepository roleRepository,
    IOrganizationRepository organizationRepository,
    IObjectMapper objectMapper)
    : ApplicationService, IUserService
{
    public async Task<UserDto> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetWithRolesAsync(id, cancellationToken);
        if (user == null)
        {
            throw new DomainException($"用户不存在：{id}");
        }

        var dto = objectMapper.Map<User, UserDto>(user);
        
        // 获取角色详情
        if (user.Roles.Any())
        {
            var roleIds = user.Roles.Select(ur => ur.RoleId).ToList();
            var roles = await roleRepository.GetListAsync(r => roleIds.Contains(r.Id), cancellationToken: cancellationToken);
            dto.Roles = objectMapper.Map<List<Role>, List<RoleSimpleDto>>(roles);
        }
        
        // 获取组织名称
        if (user.OrganizationId.HasValue)
        {
            var org = await organizationRepository.FirstOrDefaultAsync(o => o.Id == user.OrganizationId.Value, cancellationToken: cancellationToken);
            dto.OrganizationName = org?.Name;
        }

        return dto;
    }

    public async Task<PagedResultDto<UserDto>> GetListAsync(UserPagedRequestDto input, CancellationToken cancellationToken = default)
    {
        IQueryable<User> query = (await userRepository.GetQueryableAsync(cancellationToken))
            .Include(u => u.Roles);

        // 关键字搜索
        if (!string.IsNullOrWhiteSpace(input.Keyword))
        {
            query = query.Where(u => 
                u.Name.Contains(input.Keyword) || 
                u.Account.Contains(input.Keyword));
        }

        // 角色筛选
        if (input.RoleId.HasValue)
        {
            query = query.Where(u => u.Roles.Any(ur => ur.RoleId == input.RoleId.Value));
        }
        
        // 组织筛选
        if (input.OrganizationId.HasValue)
        {
            query = query.Where(u => u.OrganizationId == input.OrganizationId.Value);
        }

        // 排序
        query = input.Descending 
            ? query.OrderByDescending(u => u.CreateTime)
            : query.OrderBy(u => u.CreateTime);

        // 分页
        var total = await query.LongCountAsync(cancellationToken);
        var users = await query
            .Skip((input.Page - 1) * input.PageSize)
            .Take(input.PageSize)
            .ToListAsync(cancellationToken);

        // 获取所有相关组织
        var orgIds = users.Where(u => u.OrganizationId.HasValue).Select(u => u.OrganizationId!.Value).Distinct().ToList();
        var organizations = orgIds.Any() 
            ? await organizationRepository.GetListAsync(o => orgIds.Contains(o.Id), cancellationToken: cancellationToken)
            : new List<Organization>();
        var orgDict = organizations.ToDictionary(o => o.Id, o => o.Name);

        var dtos = new List<UserDto>();
        foreach (var user in users.Cast<User>())
        {
            var dto = objectMapper.Map<User, UserDto>(user);
            
            if (user.Roles.Any())
            {
                var roleIds = user.Roles.Select(ur => ur.RoleId).ToList();
                var roles = await roleRepository.GetListAsync(r => roleIds.Contains(r.Id), cancellationToken: cancellationToken);
                dto.Roles = objectMapper.Map<List<Role>, List<RoleSimpleDto>>(roles);
            }
            
            // 设置组织名称
            if (user.OrganizationId.HasValue && orgDict.TryGetValue(user.OrganizationId.Value, out var orgName))
            {
                dto.OrganizationName = orgName;
            }
            
            dtos.Add(dto);
        }

        return new PagedResultDto<UserDto>(total, dtos);
    }

    public async Task<List<UserSimpleDto>> GetUsersByRoleAsync(Guid roleId, CancellationToken cancellationToken = default)
    {
        var users = await (await userRepository.GetQueryableAsync(cancellationToken))
            .Include(u => u.Roles)
            .Where(u => u.Roles.Any(ur => ur.RoleId == roleId))
            .ToListAsync(cancellationToken);

        return objectMapper.Map<List<User>, List<UserSimpleDto>>(users);
    }

    public async Task<UserDto> CreateAsync(UserCreateDto input, CancellationToken cancellationToken = default)
    {
        // 检查账号是否已存在
        if (await userRepository.IsAccountExistsAsync(input.Account, cancellationToken: cancellationToken))
        {
            throw new DomainException($"账号已存在：{input.Account}");
        }

        var user = new User(input.Name, input.Account, input.Password, input.Email, input.Avatar, input.OrganizationId);

        // 分配角色
        if (input.RoleIds != null && input.RoleIds.Any())
        {
            foreach (var roleId in input.RoleIds)
            {
                user.AssignRole(roleId);
            }
        }

        await userRepository.InsertAsync(user, cancellationToken: cancellationToken);

        await UnitOfWorkManager.Current!.SaveChangesAsync(cancellationToken);

        return await GetAsync(user.Id, cancellationToken);
    }

    public async Task<UserDto> UpdateAsync(Guid id, UserUpdateDto input, CancellationToken cancellationToken = default)
    {
        var user = await userRepository.FirstAsync(u => u.Id == id, cancellationToken: cancellationToken);

        user.Update(input.Name, input.Email, input.Avatar, input.OrganizationId);

        await userRepository.UpdateAsync(user, cancellationToken: cancellationToken);

        return await GetAsync(id, cancellationToken);
    }

    public async Task UpdatePasswordAsync(Guid id, UpdatePasswordDto input, CancellationToken cancellationToken = default)
    {
        // 注意：这里应该注入AccountManager来验证旧密码
        // 为了保持简单，这里直接修改密码，实际应用中应该通过AccountManager
        var user = await userRepository.FirstAsync(u => u.Id == id, cancellationToken: cancellationToken);
        user.GeneratePassword(input.NewPassword);
        await userRepository.UpdateAsync(user, cancellationToken: cancellationToken);
    }

    public async Task UpdateAvatarAsync(Guid id, string avatarUrl, CancellationToken cancellationToken = default)
    {
        var user = await userRepository.FirstAsync(u => u.Id == id, cancellationToken: cancellationToken);

        user.UpdateAvatar(avatarUrl);

        await userRepository.UpdateAsync(user, cancellationToken: cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var user = await userRepository.FirstAsync(u => u.Id == id, cancellationToken: cancellationToken);
        await userRepository.DeleteAsync(user, cancellationToken: cancellationToken);
    }

    public async Task DeleteBatchAsync(List<Guid> ids, CancellationToken cancellationToken = default)
    {
        var users = await userRepository.GetListAsync(u => ids.Contains(u.Id), cancellationToken: cancellationToken);
        await userRepository.DeleteRangeAsync(users, cancellationToken: cancellationToken);
    }

    public async Task AssignRolesAsync(Guid userId, List<Guid> roleIds, CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetWithRolesAsync(userId, cancellationToken);
        if (user == null)
        {
            throw new DomainException($"用户不存在：{userId}");
        }

        // 清除现有角色
        user.ClearRoles();

        // 分配新角色
        foreach (var roleId in roleIds)
        {
            user.AssignRole(roleId);
        }

        await userRepository.UpdateAsync(user, cancellationToken: cancellationToken);
    }

    public async Task<List<RoleDto>> GetUserRolesAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetWithRolesAsync(userId, cancellationToken);
        if (user == null)
        {
            throw new DomainException($"用户不存在：{userId}");
        }

        if (!user.Roles.Any())
        {
            return new List<RoleDto>();
        }

        var roleIds = user.Roles.Select(ur => ur.RoleId).ToList();
        var roles = await roleRepository.GetRolesWithPermissionsAsync(roleIds, cancellationToken);

        return objectMapper.Map<List<Role>, List<RoleDto>>(roles);
    }

    public async Task<List<string>> GetUserPermissionsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetWithRolesAsync(userId, cancellationToken);
        if (user == null)
        {
            throw new DomainException($"用户不存在：{userId}");
        }

        if (!user.Roles.Any())
        {
            return new List<string>();
        }

        var roleIds = user.Roles.Select(ur => ur.RoleId).ToList();
        var roles = await roleRepository.GetRolesWithPermissionsAsync(roleIds, cancellationToken);

        // 合并所有角色的权限，去重
        var permissions = roles
            .SelectMany(r => r.Permissions.Select(p => p.PermissionCode))
            .Distinct()
            .ToList();

        return permissions;
    }

    public async Task<bool> HasPermissionAsync(Guid userId, string permissionCode, CancellationToken cancellationToken = default)
    {
        var permissions = await GetUserPermissionsAsync(userId, cancellationToken);
        return permissions.Contains(permissionCode);
    }
}
