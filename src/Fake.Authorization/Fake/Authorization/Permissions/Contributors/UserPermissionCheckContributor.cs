namespace Fake.Authorization.Permissions.Contributors;

/// <summary>
/// 基于用户的权限检查
/// </summary>
/// <param name="permissionStore"></param>
public class UserPermissionCheckContributor(IPermissionStore permissionStore)
    : PermissionCheckContributorBase(permissionStore)
{
    public override string PermissionOwner => "User";

    public override async Task<bool> IsGrantedAsync(ClaimsPrincipal? user, PermissionDto permission)
    {
        ThrowHelper.ThrowIfNull(permission, nameof(permission));

        var userId = user?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId == null) return false;

        return await PermissionStore.IsGrantedAsync(permission.Name, PermissionOwner, userId);
    }
}