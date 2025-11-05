using Fake.Domain;
using Fake.Domain.Exceptions;
using Fake.Helpers;
using Fake.Rbac.Domain.UserAggregate;

namespace Fake.Rbac.Domain.Managers;

/// <summary>
/// 账号管理领域服务
/// </summary>
public class AccountManager : DomainService
{
    private readonly IUserRepository _userRepository;

    public AccountManager(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    /// <summary>
    /// 注册新用户
    /// </summary>
    public async Task<User> RegisterAsync(string name, string account, string password, string? email = null, CancellationToken cancellationToken = default)
    {
        // 验证账号唯一性
        await ValidateAccountUniqueAsync(account, cancellationToken: cancellationToken);

        // 验证密码强度
        ValidatePasswordStrength(password);

        var user = new User(name, account, password, email);
        await _userRepository.InsertAsync(user, cancellationToken: cancellationToken);

        return user;
    }

    /// <summary>
    /// 验证用户登录凭证
    /// </summary>
    public async Task<User> ValidateCredentialsAsync(string account, string password, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.FindByAccountAsync(account, cancellationToken);

        if (user == null)
        {
            throw new DomainException("用户不存在或密码错误");
        }

        var encryptedPassword = MD5Helper.GeneratePassword(password, user.EncryptPassword.Salt);
        if (!user.EncryptPassword.Password.Equals(encryptedPassword))
        {
            throw new DomainException("用户不存在或密码错误");
        }

        return user;
    }

    /// <summary>
    /// 修改用户密码
    /// </summary>
    public async Task UpdatePasswordAsync(Guid userId, string oldPassword, string newPassword, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.FirstAsync(u => u.Id == userId, cancellationToken: cancellationToken);

        // 验证旧密码
        var encryptedOldPassword = MD5Helper.GeneratePassword(oldPassword, user.EncryptPassword.Salt);
        if (!user.EncryptPassword.Password.Equals(encryptedOldPassword))
        {
            throw new DomainException("旧密码不正确");
        }

        // 验证新密码强度
        ValidatePasswordStrength(newPassword);

        // 生成新密码
        user.GeneratePassword(newPassword);

        await _userRepository.UpdateAsync(user, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// 重置用户密码（管理员操作）
    /// </summary>
    public async Task ResetPasswordAsync(Guid userId, string newPassword, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.FirstAsync(u => u.Id == userId, cancellationToken: cancellationToken);

        // 验证新密码强度
        ValidatePasswordStrength(newPassword);

        // 生成新密码
        user.GeneratePassword(newPassword);

        await _userRepository.UpdateAsync(user, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// 校验账号唯一性
    /// </summary>
    public async Task ValidateAccountUniqueAsync(string account, Guid? excludeUserId = null, CancellationToken cancellationToken = default)
    {
        var exists = await _userRepository.IsAccountExistsAsync(account, excludeUserId, cancellationToken);
        if (exists)
        {
            throw new DomainException($"账号已存在：{account}");
        }
    }

    /// <summary>
    /// 校验密码强度
    /// </summary>
    public void ValidatePasswordStrength(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            throw new DomainException("密码不能为空");
        }

        if (password.Length < 6)
        {
            throw new DomainException("密码长度不能少于6位");
        }

        if (password.Length > 100)
        {
            throw new DomainException("密码长度不能超过100位");
        }

        // TODO: 可以添加更复杂的密码强度验证规则
        // 例如：必须包含大小写字母、数字、特殊字符等
    }

    /// <summary>
    /// 检查密码是否正确
    /// </summary>
    public bool CheckPassword(User user, string password)
    {
        var encryptedPassword = MD5Helper.GeneratePassword(password, user.EncryptPassword.Salt);
        return user.EncryptPassword.Password.Equals(encryptedPassword);
    }
}
