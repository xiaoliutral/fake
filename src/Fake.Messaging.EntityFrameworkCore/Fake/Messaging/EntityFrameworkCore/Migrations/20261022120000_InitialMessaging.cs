using System;
using Fake.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fake.Messaging.EntityFrameworkCore.Migrations;

/// <summary>
/// Messaging 基线：msg_outbox / msg_inbox（经 FakeDbProperties 加前缀，默认 fk_）。
/// </summary>
public partial class InitialMessaging : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        string T(string n) => FakeDbProperties.GetTableName(n);

        migrationBuilder.CreateTable(
            name: T("msg_outbox"),
            columns: table => new
            {
                MessageId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                Destination = table.Column<string>(type: "varchar(512)", maxLength: 512, nullable: false),
                Kind = table.Column<int>(type: "int", nullable: false),
                Body = table.Column<string>(type: "longtext", nullable: false),
                TypeName = table.Column<string>(type: "varchar(512)", maxLength: 512, nullable: true),
                TransactionId = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false),
                HeadersJson = table.Column<string>(type: "longtext", nullable: false),
                State = table.Column<int>(type: "int", nullable: false),
                TimesSent = table.Column<int>(type: "int", nullable: false),
                CreationTime = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                LockExpiresAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                LastError = table.Column<string>(type: "longtext", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_" + T("msg_outbox"), x => x.MessageId);
            });

        migrationBuilder.CreateTable(
            name: T("msg_inbox"),
            columns: table => new
            {
                MessageId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                ConsumerGroup = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: false),
                Kind = table.Column<int>(type: "int", nullable: false),
                Body = table.Column<string>(type: "longtext", nullable: false),
                TypeName = table.Column<string>(type: "varchar(512)", maxLength: 512, nullable: true),
                Destination = table.Column<string>(type: "varchar(512)", maxLength: 512, nullable: true),
                HeadersJson = table.Column<string>(type: "longtext", nullable: false),
                State = table.Column<int>(type: "int", nullable: false),
                ProcessedTime = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                ErrorMessage = table.Column<string>(type: "longtext", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_" + T("msg_inbox"), x => new { x.MessageId, x.ConsumerGroup });
            });

        migrationBuilder.CreateIndex(
            name: "ix_" + T("msg_outbox") + "_state_creation_time",
            table: T("msg_outbox"),
            columns: new[] { "State", "CreationTime" });

        migrationBuilder.CreateIndex(
            name: "ix_" + T("msg_outbox") + "_transaction_id",
            table: T("msg_outbox"),
            column: "TransactionId");

        migrationBuilder.CreateIndex(
            name: "ix_" + T("msg_outbox") + "_kind",
            table: T("msg_outbox"),
            column: "Kind");

        migrationBuilder.CreateIndex(
            name: "ix_" + T("msg_inbox") + "_processed_time",
            table: T("msg_inbox"),
            column: "ProcessedTime");

        migrationBuilder.CreateIndex(
            name: "ix_" + T("msg_inbox") + "_kind",
            table: T("msg_inbox"),
            column: "Kind");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        string T(string n) => FakeDbProperties.GetTableName(n);
        migrationBuilder.DropTable(name: T("msg_inbox"));
        migrationBuilder.DropTable(name: T("msg_outbox"));
    }
}
