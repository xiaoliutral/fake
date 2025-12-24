using Fake.Domain.Entities.Auditing;
using Fake.Helpers;
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
    public EncryptPassword EncryptPassword { get; private set; }
    public string? Email { get; private set; }
    public string? Avatar { get; private set; }
    
    /// <summary>
    /// 所属组织ID
    /// </summary>
    public Guid? OrganizationId { get; private set; }
    
    public IReadOnlyCollection<UserRole> Roles => _roles.AsReadOnly();

    private readonly List<UserRole> _roles = new();

    public User()
    {
        
    }
    
    public User(string name, string account, string password, string? email = null, string? avatar = null, Guid? organizationId = null)
    {
        Name = name;
        Account = account;
        GeneratePassword(password);
        Email = email;
        Avatar = avatar;
        OrganizationId = organizationId;
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

    public void GeneratePassword(string password)
    {
        var salt = MD5Helper.GenerateSalt();
        var pwd = MD5Helper.GeneratePassword(password, salt);
        EncryptPassword = new EncryptPassword(pwd, salt);
    }

    public void Update(string? name = null, string? email = null, string? avatar = null, Guid? organizationId = null)
    {
        if (!string.IsNullOrWhiteSpace(name))
        {
            Name = name;
        }

        if (email != null)
        {
            Email = email;
        }

        if (avatar != null)
        {
            Avatar = avatar;
        }
        
        OrganizationId = organizationId;
    }

    public void UpdateAvatar(string? avatar)
    {
        Avatar = avatar;
    }
    
    public void SetOrganization(Guid? organizationId)
    {
        OrganizationId = organizationId;
    }
}