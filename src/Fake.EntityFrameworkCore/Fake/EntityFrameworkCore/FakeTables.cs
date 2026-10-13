namespace Fake.EntityFrameworkCore;

/// <summary>
/// Fake 模块常用逻辑表名 → 物理表名（含 <see cref="FakeDbProperties.DbTablePrefix"/>，默认 <c>fk_</c>）。
/// 宿主手写 SQL 时请用此类，勿硬编码 <c>user</c>/<c>role</c> 等。
/// </summary>
public static class FakeTables
{
    public static string User => FakeDbProperties.GetTableName("user");
    public static string Role => FakeDbProperties.GetTableName("role");
    public static string Menu => FakeDbProperties.GetTableName("menu");
    public static string Organization => FakeDbProperties.GetTableName("organization");
    public static string UserRole => FakeDbProperties.GetTableName("user_role");
    public static string RolePermission => FakeDbProperties.GetTableName("role_permission");
}
