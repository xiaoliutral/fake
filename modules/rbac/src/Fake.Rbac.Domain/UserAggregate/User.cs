using Fake.Domain.Entities.Auditing;
using Fake.Rbac.Domain.RoleAggregate;

namespace Fake.Rbac.Domain.UserAggregate;

public class User: FullAuditedAggregateRoot<Guid>
{
    /// <summary>
    /// 名称
    /// </summary>
    public string Name { get; private set; }
    
    /// <summary>
    /// 账号
    /// </summary>
    public string Account { get; private set; }
    
    /// <summary>
    /// 密码
    /// </summary>
    public EncryptPassword Password { get; private set; }
    public string? Email { get; private set; }
    public string? Avatar { get; private set; }
    
    public IReadOnlyCollection<Role> Roles => _roles.AsReadOnly();

    private readonly List<Role> _roles = new();

    public User()
    {
        
    }
    
    public User(string name, string account, EncryptPassword password, string? email = null, string? avatar = null)
    {
        Name = name;
        Account = account;
        Password = password;
        Email = email;
        Avatar = avatar;
    }

}