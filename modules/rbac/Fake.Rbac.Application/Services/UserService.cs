using Fake.Application;
using Fake.Domain.Exceptions;
using Fake.ObjectMapping;
using Fake.Rbac.Application.Dtos.Common;
using Fake.Rbac.Application.Dtos.Role;
using Fake.Rbac.Application.Dtos.User;
using Fake.Rbac.Domain.RoleAggregate;
using Fake.Rbac.Domain.UserAggregate;
using Fake.Rbac.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Fake.Rbac.Application.Services;

public class UserService : ApplicationService, IUserService
{
    private readonly IEfCoreUserRepository _userRepository;
    private readonly IEfCoreRoleRepository _roleRepository;
    private readonly IObjectMapper _objectMapper;

    public UserService(
        IEfCoreUserRepository userRepository,
        IEfCoreRoleRepository roleRepository,
        IObjectMapper objectMapper)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _objectMapper = objectMapper;
    }

    public async Task<UserDto> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetWithRolesAsync(id, cancellationToken);
        if (user == null)
        {
            throw new DomainException($"用户不存在：{id}");
        }

        var dto = _objectMapper.Map<User, UserDto>(user);
        
        // 获取角色详情
        if (user.Roles.Any())
        {
            var roleIds = user.Roles.Select(ur => ur.RoleId).ToList();
            var roles = await _roleRepository.GetListAsync(r => roleIds.Contains(r.Id), cancellationToken: cancellationToken);
            dto.Roles = _objectMapper.Map<List<Role>, List<RoleSimpleDto>>(roles);
        }

        return dto;
    }

    public async Task<PagedResultDto<UserDto>> GetListAsync(UserPagedRequestDto input, CancellationToken cancellationToken = default)
    {
        IQueryable<User> query = (await _userRepository.GetQueryableAsync(cancellationToken))
            .Include(u => ((User)u).Roles);

        // 关键字搜索
        if (!string.IsNullOrWhiteSpace(input.Keyword))
        {
            query = query.Where(u => 
                ((User)u).Name.Contains(input.Keyword) || 
                ((User)u).Account.Contains(input.Keyword));
        }

        // 角色筛选
        if (input.RoleId.HasValue)
        {
            query = query.Where(u => ((User)u).Roles.Any(ur => ur.RoleId == input.RoleId.Value));
        }

        // 排序
        query = input.Descending 
            ? query.OrderByDescending(u => ((User)u).CreateTime)
            : query.OrderBy(u => ((User)u).CreateTime);

        // 分页
        var total = await query.LongCountAsync(cancellationToken);
        var users = await query
            .Skip((input.Page - 1) * input.PageSize)
            .Take(input.PageSize)
            .ToListAsync(cancellationToken);

        var dtos = new List<UserDto>();
        foreach (var user in users.Cast<User>())
        {
            var dto = _objectMapper.Map<User, UserDto>(user);
            
            if (user.Roles.Any())
            {
                var roleIds = user.Roles.Select(ur => ur.RoleId).ToList();
                var roles = await _roleRepository.GetListAsync(r => roleIds.Contains(r.Id), cancellationToken: cancellationToken);
                dto.Roles = _objectMapper.Map<List<Role>, List<RoleSimpleDto>>(roles);
            }
            
            dtos.Add(dto);
        }

        return new PagedResultDto<UserDto>(total, dtos);
    }

    public async Task<List<UserSimpleDto>> GetUsersByRoleAsync(Guid roleId, CancellationToken cancellationToken = default)
    {
        var users = await (await _userRepository.GetQueryableAsync(cancellationToken))
            .Include(u => ((User)u).Roles)
            .Where(u => ((User)u).Roles.Any(ur => ur.RoleId == roleId))
            .ToListAsync(cancellationToken);

        return _objectMapper.Map<List<User>, List<UserSimpleDto>>(users.Cast<User>().ToList());
    }

    public async Task<UserDto> CreateAsync(UserCreateDto input, CancellationToken cancellationToken = default)
    {
        // 检查账号是否已存在
        if (await _userRepository.IsAccountExistsAsync(input.Account, cancellationToken: cancellationToken))
        {
            throw new DomainException($"账号已存在：{input.Account}");
        }

        var user = new User(input.Name, input.Account, input.Password, input.Email, input.Avatar);

        // 分配角色
        if (input.RoleIds != null && input.RoleIds.Any())
        {
            foreach (var roleId in input.RoleIds)
            {
                user.AssignRole(roleId);
            }
        }

        await _userRepository.InsertAsync(user, cancellationToken: cancellationToken);

        return await GetAsync(user.Id, cancellationToken);
    }

    public async Task<UserDto> UpdateAsync(Guid id, UserUpdateDto input, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.FirstAsync(u => u.Id == id, cancellationToken: cancellationToken);

        user.Update(input.Name, input.Email, input.Avatar);

        await _userRepository.UpdateAsync(user, cancellationToken: cancellationToken);

        return await GetAsync(id, cancellationToken);
    }

    public async Task UpdatePasswordAsync(Guid id, UpdatePasswordDto input, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.FirstAsync(u => u.Id == id, cancellationToken: cancellationToken);

        // 验证旧密码（需要在领域服务中实现）
        // 这里简化处理，实际应该在 AccountManager 中验证
        user.GeneratePassword(input.NewPassword);

        await _userRepository.UpdateAsync(user, cancellationToken: cancellationToken);
    }

    public async Task UpdateAvatarAsync(Guid id, string avatarUrl, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.FirstAsync(u => u.Id == id, cancellationToken: cancellationToken);

        user.UpdateAvatar(avatarUrl);

        await _userRepository.UpdateAsync(user, cancellationToken: cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.FirstAsync(u => u.Id == id, cancellationToken: cancellationToken);
        await _userRepository.DeleteAsync(user, cancellationToken: cancellationToken);
    }

    public async Task DeleteBatchAsync(List<Guid> ids, CancellationToken cancellationToken = default)
    {
        var users = await _userRepository.GetListAsync(u => ids.Contains(u.Id), cancellationToken: cancellationToken);
        await _userRepository.DeleteRangeAsync(users, cancellationToken: cancellationToken);
    }

    public async Task AssignRolesAsync(Guid userId, List<Guid> roleIds, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetWithRolesAsync(userId, cancellationToken);
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

        await _userRepository.UpdateAsync(user, cancellationToken: cancellationToken);
    }

    public async Task<List<RoleDto>> GetUserRolesAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetWithRolesAsync(userId, cancellationToken);
        if (user == null)
        {
            throw new DomainException($"用户不存在：{userId}");
        }

        if (!user.Roles.Any())
        {
            return new List<RoleDto>();
        }

        var roleIds = user.Roles.Select(ur => ur.RoleId).ToList();
        var roles = await _roleRepository.GetRolesWithPermissionsAsync(roleIds, cancellationToken);

        return _objectMapper.Map<List<Role>, List<RoleDto>>(roles);
    }

    public async Task<List<string>> GetUserPermissionsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetWithRolesAsync(userId, cancellationToken);
        if (user == null)
        {
            throw new DomainException($"用户不存在：{userId}");
        }

        if (!user.Roles.Any())
        {
            return new List<string>();
        }

        var roleIds = user.Roles.Select(ur => ur.RoleId).ToList();
        var roles = await _roleRepository.GetRolesWithPermissionsAsync(roleIds, cancellationToken);

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
