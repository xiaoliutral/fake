using Fake.Domain.Entities.Auditing;

namespace Fake.Rbac.Domain.RoleAggregate;

/// <summary>
/// 角色权限
/// </summary>
public class RolePermission: CreateAuditedEntity<Guid>
{
    public Guid RoleId { get; private set; }
    
    public string PermissionCode { get; private set; }


    public RolePermission()
    {
        
    }

    public RolePermission(Guid roleId, string permissionCode)
    {
        RoleId = roleId;
        PermissionCode = permissionCode;
    }
}