using Fake.Domain.Entities.Auditing;

namespace Fake.Rbac.Domain.RoleAggregate;

public class Role: FullAuditedAggregateRoot<Guid>
{
    public string Name { get; private set; }

    public string Code { get; private set; }
    
    public IReadOnlyCollection<RolePermission> Permissions => _permissions.AsReadOnly();
    private readonly List<RolePermission> _permissions = new();

    public Role()
    {
        
    }
    
    public Role(string name, string code)
    {
        Name = name;
        Code = code;
    }

    public void Update(string? name = null)
    {
        if (!string.IsNullOrWhiteSpace(name))
        {
            Name = name;
        }
    }

    public void AddPermission(string permissionCode)
    {
        var permission = _permissions.SingleOrDefault(p => p.PermissionCode == permissionCode);
        if (permission == null)
        {
            _permissions.Add(new RolePermission(Id, permissionCode));
        }
    }

    public void RemovePermission(string permissionCode)
    {
        var permission = _permissions.SingleOrDefault(p => p.PermissionCode == permissionCode);
        if (permission != null)
        {
            _permissions.Remove(permission);
        }
    }

    public void ClearPermissions()
    {
        _permissions.Clear();
    }

    public void SetPermissions(List<string> permissionCodes)
    {
        _permissions.Clear();
        foreach (var permissionCode in permissionCodes)
        {
            _permissions.Add(new RolePermission(Id, permissionCode));
        }
    }
}
