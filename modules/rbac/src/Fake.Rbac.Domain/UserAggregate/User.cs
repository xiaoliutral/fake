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
    
    public IReadOnlyCollection<UserRole> Roles => _roles.AsReadOnly();

    private readonly List<UserRole> _roles = new();

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

    public void AssignRole(Guid roleId)
    {
        if (_roles.Any(ur => ur.RoleId == roleId))
        {
            return;
        }
        
        _roles.Add(new UserRole(Id, roleId));
    }
    
    public void RemoveRole(Guid roleId)
    {
        var userRole = _roles.FirstOrDefault(ur => ur.RoleId == roleId);
        if (userRole != null)
        {
            _roles.Remove(userRole);
        }
    }
    
    public void ClearRoles()
    {
        _roles.Clear();
    }
    
    public bool HasRole(Guid roleId)
    {
        return _roles.Any(ur => ur.RoleId == roleId);
    }
}