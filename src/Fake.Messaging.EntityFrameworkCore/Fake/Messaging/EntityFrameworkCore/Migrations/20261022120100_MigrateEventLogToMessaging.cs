using Fake.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fake.Messaging.EntityFrameworkCore.Migrations;

/// <summary>
/// 将历史 fk_event_outbox / fk_event_inbox 拷贝到 fk_msg_* 后删除旧表。
/// 旧表不存在则整段跳过（新环境仅有 msg_*）；须用动态 SQL，否则 FROM 旧表在解析阶段就会失败。
/// </summary>
public partial class MigrateEventLogToMessaging : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        string T(string n) => FakeDbProperties.GetTableName(n);
        var eventOutbox = T("event_outbox");
        var eventInbox = T("event_inbox");
        var msgOutbox = T("msg_outbox");
        var msgInbox = T("msg_inbox");

        // 空串在「外层单引号字符串」里要写成四个 ' → 预编译后变成 SQL 的 ''
        const string emptySql = "''''";

        // MySQL：表不存在时不能写死 FROM `fk_event_*`，否则即使 WHERE EXISTS 也会在解析阶段报错。
        migrationBuilder.Sql($@"
SET @__fk_event_outbox_exists := (
    SELECT COUNT(1) FROM information_schema.tables
    WHERE table_schema = DATABASE() AND table_name = '{eventOutbox}'
);
SET @__sql := IF(
    @__fk_event_outbox_exists > 0,
    'INSERT INTO `{msgOutbox}`
        (`MessageId`, `Destination`, `Kind`, `Body`, `TypeName`, `TransactionId`, `HeadersJson`,
         `State`, `TimesSent`, `CreationTime`, `LockExpiresAt`, `LastError`)
     SELECT
        `EventId`,
        CONCAT(''eventbus:'', COALESCE(`EventTypeName`, {emptySql})),
        0,
        `Content`,
        `EventTypeName`,
        `TransactionId`,
        ''{{}}'',
        `State`,
        `TimesSent`,
        `CreationTime`,
        `LockExpiresAt`,
        NULL
     FROM `{eventOutbox}` src
     WHERE NOT EXISTS (
         SELECT 1 FROM `{msgOutbox}` m WHERE m.`MessageId` = src.`EventId`
     )',
    'SELECT 1');
PREPARE __stmt FROM @__sql;
EXECUTE __stmt;
DEALLOCATE PREPARE __stmt;
");

        migrationBuilder.Sql($@"
SET @__fk_event_inbox_exists := (
    SELECT COUNT(1) FROM information_schema.tables
    WHERE table_schema = DATABASE() AND table_name = '{eventInbox}'
);
SET @__sql := IF(
    @__fk_event_inbox_exists > 0,
    'INSERT INTO `{msgInbox}`
        (`MessageId`, `ConsumerGroup`, `Kind`, `Body`, `TypeName`, `Destination`, `HeadersJson`,
         `State`, `ProcessedTime`, `ErrorMessage`)
     SELECT
        `EventId`,
        ''eventbus'',
        0,
        `Content`,
        `EventTypeName`,
        CONCAT(''eventbus:'', COALESCE(`EventTypeName`, {emptySql})),
        ''{{}}'',
        `State`,
        `ProcessedTime`,
        `ErrorMessage`
     FROM `{eventInbox}` src
     WHERE NOT EXISTS (
         SELECT 1 FROM `{msgInbox}` m
         WHERE m.`MessageId` = src.`EventId` AND m.`ConsumerGroup` = ''eventbus''
     )',
    'SELECT 1');
PREPARE __stmt FROM @__sql;
EXECUTE __stmt;
DEALLOCATE PREPARE __stmt;
");

        migrationBuilder.Sql($@"
DROP TABLE IF EXISTS `{eventInbox}`;
DROP TABLE IF EXISTS `{eventOutbox}`;
");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // 不可逆：历史 event_* 表已删除
    }
}
