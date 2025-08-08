using Fake.Domain;
using Fake.Domain.Exceptions;
using Fake.Helpers;
using Fake.Rbac.Domain.UserAggregate;

namespace Fake.Rbac.Domain.Managers;

public class AccountManager(IUserRepository userRepository) : DomainService
{
    public async Task Register(string username, string account, string password)
    {
        var user = new User(username, account, password);
        await userRepository.InsertAsync(user);
    }

    public async Task LoginCheck(string account, string password)
    {
        var user = await userRepository.FirstOrDefaultAsync(x => x.Account == account);

        if (user == null || !user.Account.Equals(account)) throw new DomainException("用户不存在或密码错误");

        if (!user.EncryptPassword.Password.Equals(MD5Helper.GeneratePassword(password, user.EncryptPassword.Salt)))
            throw new DomainException("用户不存在或密码错误");
    }

    public async Task UpdatePassword(Guid userId, string password, string newPassword)
    {
        var user = await userRepository.FirstOrDefaultAsync(x => x.Id == userId);

        if (user == null ||
            !user.EncryptPassword.Password.Equals(MD5Helper.GeneratePassword(password, user.EncryptPassword.Salt)))
            throw new DomainException("用户不存在或密码错误");

        user.GeneratePassword(newPassword);
        
        await userRepository.UpdateAsync(user);
    }
}