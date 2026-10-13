using System;
using Fake.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fake.EventBus.IntegrationEventLog.Migrations;

/// <summary>
/// IntegrationEventLog 基线：event_outbox / event_inbox（经 FakeDbProperties 加前缀，默认 fk_）。
/// </summary>
public partial class InitialCreate : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        string T(string n) => FakeDbProperties.GetTableName(n);

        migrationBuilder.CreateTable(
                name: T("event_outbox"),
                columns: table => new
                {
                    EventId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    EventTypeName = table.Column<string>(type: "varchar(512)", maxLength: 512, nullable: false),
                    State = table.Column<int>(type: "int", nullable: false),
                    TimesSent = table.Column<int>(type: "int", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Content = table.Column<string>(type: "longtext", nullable: false),
                    TransactionId = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false),
                    LockExpiresAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_" + T("event_outbox"), x => x.EventId);
                });

        migrationBuilder.CreateTable(
                name: T("event_inbox"),
                columns: table => new
                {
                    EventId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    EventTypeName = table.Column<string>(type: "varchar(512)", maxLength: 512, nullable: false),
                    State = table.Column<int>(type: "int", nullable: false),
                    ProcessedTime = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Content = table.Column<string>(type: "longtext", nullable: false),
                    ErrorMessage = table.Column<string>(type: "longtext", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_" + T("event_inbox"), x => x.EventId);
                });

        migrationBuilder.CreateIndex(
            name: "ix_" + T("event_outbox") + "_state_creation_time",
            table: T("event_outbox"),
            columns: new[] { "State", "CreationTime" });

        migrationBuilder.CreateIndex(
            name: "ix_" + T("event_outbox") + "_transaction_id",
            table: T("event_outbox"),
            column: "TransactionId");

        migrationBuilder.CreateIndex(
            name: "ix_" + T("event_inbox") + "_processed_time",
            table: T("event_inbox"),
            column: "ProcessedTime");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        string T(string n) => FakeDbProperties.GetTableName(n);

        migrationBuilder.DropTable(name: T("event_inbox"));
        migrationBuilder.DropTable(name: T("event_outbox"));
    }
}
