using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fake.Rbac.Infrastructure.Migrations;

/// <summary>
/// 关联表也去掉唯一索引（若上一迁移已执行过但未含本段，可幂等再跑）。
/// </summary>
[DbContext(typeof(FakeRbacDbContext))]
[Migration("20261009110000_DropAssociationTableUniqueIndexes")]
public partial class DropAssociationTableUniqueIndexes : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DROP PROCEDURE IF EXISTS `fake_drop_index_if_exists`;
            """);

        migrationBuilder.Sql("""
            CREATE PROCEDURE `fake_drop_index_if_exists`(IN tbl VARCHAR(64), IN idx VARCHAR(64))
            BEGIN
              IF EXISTS (
                SELECT 1 FROM information_schema.statistics
                WHERE table_schema = DATABASE() AND table_name = tbl AND index_name = idx
              ) THEN
                SET @sql := CONCAT('DROP INDEX `', idx, '` ON `', tbl, '`');
                PREPARE stmt FROM @sql;
                EXECUTE stmt;
                DEALLOCATE PREPARE stmt;
              END IF;
            END
            """);

        migrationBuilder.Sql("""
            CALL fake_drop_index_if_exists('role_permission', 'IX_role_permission_RoleId_PermissionCode');
            CALL fake_drop_index_if_exists('user_role', 'IX_user_role_UserId_RoleId');

            CREATE INDEX `IX_role_permission_RoleId_PermissionCode` ON `role_permission` (`RoleId`, `PermissionCode`);
            CREATE INDEX `IX_user_role_UserId_RoleId` ON `user_role` (`UserId`, `RoleId`);

            DROP PROCEDURE IF EXISTS `fake_drop_index_if_exists`;
            """);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_role_permission_RoleId_PermissionCode",
            table: "role_permission");

        migrationBuilder.DropIndex(
            name: "IX_user_role_UserId_RoleId",
            table: "user_role");

        migrationBuilder.CreateIndex(
            name: "IX_role_permission_RoleId_PermissionCode",
            table: "role_permission",
            columns: new[] { "RoleId", "PermissionCode" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_user_role_UserId_RoleId",
            table: "user_role",
            columns: new[] { "UserId", "RoleId" },
            unique: true);
    }
}
