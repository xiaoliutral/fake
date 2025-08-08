using Fake.Application;
using Fake.Rbac.Application.Dtos.User;
using Fake.Rbac.Domain.UserAggregate;

namespace Fake.Rbac.Application.Services;

public class UserService(IUserRepository userRepository):ApplicationService
{
    /// <summary>
    /// 极速注册
    /// </summary>
    /// <param name="input"></param>
    public async Task Create(UserCreateInput input)
    {
        var user = new User(input.Username, input.Username, input.Password);
        await userRepository.InsertAsync(user);
    }
}