using Fake.Domain;

namespace Fake.Rbac.Domain.UserAggregate;

public class EncryptPassword: ValueObject
{
    /// <summary>
    /// 密码
    /// </summary>
    public string Password { get; private set; }

    /// <summary>
    /// 加密盐值
    /// </summary>
    public string Salt { get; private set; }

    public EncryptPassword()
    {
    }

    public EncryptPassword(string password, string salt)
    {
        Password = password;
        Salt = salt;
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Password;
        yield return Salt;
    }
}