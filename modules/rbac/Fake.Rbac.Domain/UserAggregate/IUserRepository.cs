using Fake.Domain.Repositories;

namespace Fake.Rbac.Domain.UserAggregate;

public interface IUserRepository: IRepository<User>
{
    public Task<IQueryable<User>> GetQueryableAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 根据账号查找用户
    /// </summary>
    Task<User?> FindByAccountAsync(string account, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 获取用户及其角色
    /// </summary>
    Task<User?> GetWithRolesAsync(Guid userId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 批量获取用户及其角色
    /// </summary>
    Task<List<User>> GetUsersWithRolesAsync(List<Guid> userIds, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 检查账号是否已存在
    /// </summary>
    Task<bool> IsAccountExistsAsync(string account, Guid? excludeUserId = null, CancellationToken cancellationToken = default);
}