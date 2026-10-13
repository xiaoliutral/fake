using Fake.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fake.Rbac.Infrastructure.Migrations;

/// <summary>
/// 存量库：无前缀表 → <see cref="FakeDbProperties"/> 前缀（默认 fk_）。
/// 新库若 InitialCreate 已直接建出带前缀表，本迁移幂等跳过。
/// </summary>
public partial class RenameTablesToFakePrefix : Migration
{
    private static readonly string[] LogicalNames =
    [
        "menu",
        "organization",
        "role",
        "user",
        "role_permission",
        "user_role"
    ];

    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        foreach (var logical in LogicalNames)
            RenameIfNeeded(migrationBuilder, from: logical, to: FakeDbProperties.GetTableName(logical));
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        foreach (var logical in LogicalNames)
            RenameIfNeeded(migrationBuilder, from: FakeDbProperties.GetTableName(logical), to: logical);
    }

    private static void RenameIfNeeded(MigrationBuilder migrationBuilder, string from, string to)
    {
        if (string.Equals(from, to, StringComparison.OrdinalIgnoreCase))
            return;

        // MySQL：仅当旧表存在且新表不存在时 RENAME，避免新库重复执行失败
        migrationBuilder.Sql(
            $$"""
            SET @fk_old := '{{from}}';
            SET @fk_new := '{{to}}';
            SET @fk_schema := DATABASE();
            SET @fk_old_exists := (
              SELECT COUNT(1) FROM information_schema.tables
              WHERE table_schema = @fk_schema AND table_name = @fk_old
            );
            SET @fk_new_exists := (
              SELECT COUNT(1) FROM information_schema.tables
              WHERE table_schema = @fk_schema AND table_name = @fk_new
            );
            SET @fk_sql := IF(
              @fk_old_exists = 1 AND @fk_new_exists = 0,
              CONCAT('RENAME TABLE `', @fk_old, '` TO `', @fk_new, '`'),
              'SELECT 1'
            );
            PREPARE fk_stmt FROM @fk_sql;
            EXECUTE fk_stmt;
            DEALLOCATE PREPARE fk_stmt;
            """);
    }
}
