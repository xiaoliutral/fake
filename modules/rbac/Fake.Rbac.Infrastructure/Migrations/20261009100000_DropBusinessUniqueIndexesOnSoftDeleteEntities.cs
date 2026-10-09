using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fake.Rbac.Infrastructure.Migrations;

/// <summary>
/// 去掉业务唯一索引，改为普通索引；唯一性改由应用层保证。
/// </summary>
[DbContext(typeof(FakeRbacDbContext))]
[Migration("20261009100000_DropBusinessUniqueIndexesOnSoftDeleteEntities")]
public partial class DropBusinessUniqueIndexesOnSoftDeleteEntities : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DROP PROCEDURE IF EXISTS `fake_drop_index_if_exists`;
            DROP PROCEDURE IF EXISTS `fake_drop_column_if_exists`;
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
            CREATE PROCEDURE `fake_drop_column_if_exists`(IN tbl VARCHAR(64), IN col VARCHAR(64))
            BEGIN
              IF EXISTS (
                SELECT 1 FROM information_schema.columns
                WHERE table_schema = DATABASE() AND table_name = tbl AND column_name = col
              ) THEN
                SET @sql := CONCAT('ALTER TABLE `', tbl, '` DROP COLUMN `', col, '`');
                PREPARE stmt FROM @sql;
                EXECUTE stmt;
                DEALLOCATE PREPARE stmt;
              END IF;
            END
            """);

        migrationBuilder.Sql("""
            CALL fake_drop_index_if_exists('user', 'IX_user_Account_DeletionToken');
            CALL fake_drop_index_if_exists('user', 'IX_user_Email_DeletionToken');
            CALL fake_drop_index_if_exists('role', 'IX_role_Code_DeletionToken');
            CALL fake_drop_index_if_exists('organization', 'IX_organization_Code_DeletionToken');
            CALL fake_drop_index_if_exists('menu', 'IX_menu_PermissionCode_DeletionToken');

            CALL fake_drop_index_if_exists('user', 'IX_user_Account');
            CALL fake_drop_index_if_exists('user', 'IX_user_Email');
            CALL fake_drop_index_if_exists('role', 'IX_role_Code');
            CALL fake_drop_index_if_exists('organization', 'IX_organization_Code');
            CALL fake_drop_index_if_exists('menu', 'IX_menu_PermissionCode');
            CALL fake_drop_index_if_exists('role_permission', 'IX_role_permission_RoleId_PermissionCode');
            CALL fake_drop_index_if_exists('user_role', 'IX_user_role_UserId_RoleId');

            CALL fake_drop_column_if_exists('user', 'DeletionToken');
            CALL fake_drop_column_if_exists('role', 'DeletionToken');
            CALL fake_drop_column_if_exists('organization', 'DeletionToken');
            CALL fake_drop_column_if_exists('menu', 'DeletionToken');

            CREATE INDEX `IX_user_Account` ON `user` (`Account`);
            CREATE INDEX `IX_user_Email` ON `user` (`Email`);
            CREATE INDEX `IX_role_Code` ON `role` (`Code`);
            CREATE INDEX `IX_organization_Code` ON `organization` (`Code`);
            CREATE INDEX `IX_menu_PermissionCode` ON `menu` (`PermissionCode`);
            CREATE INDEX `IX_role_permission_RoleId_PermissionCode` ON `role_permission` (`RoleId`, `PermissionCode`);
            CREATE INDEX `IX_user_role_UserId_RoleId` ON `user_role` (`UserId`, `RoleId`);

            DROP PROCEDURE IF EXISTS `fake_drop_index_if_exists`;
            DROP PROCEDURE IF EXISTS `fake_drop_column_if_exists`;
            """);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(name: "IX_user_Account", table: "user");
        migrationBuilder.DropIndex(name: "IX_user_Email", table: "user");
        migrationBuilder.DropIndex(name: "IX_role_Code", table: "role");
        migrationBuilder.DropIndex(name: "IX_organization_Code", table: "organization");
        migrationBuilder.DropIndex(name: "IX_menu_PermissionCode", table: "menu");

        migrationBuilder.CreateIndex(
            name: "IX_user_Account",
            table: "user",
            column: "Account",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_user_Email",
            table: "user",
            column: "Email",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_role_Code",
            table: "role",
            column: "Code",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_organization_Code",
            table: "organization",
            column: "Code",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_menu_PermissionCode",
            table: "menu",
            column: "PermissionCode");
    }
}
