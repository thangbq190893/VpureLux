using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VPureLux.Migrations
{
    /// <inheritdoc />
    public partial class AddAuditModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AppBusinessAuditLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EventId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Module = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    EventName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    EventVersion = table.Column<int>(type: "int", nullable: false),
                    Action = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EntityType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EntityDisplay = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    ActorType = table.Column<byte>(type: "tinyint", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UserNameSnapshot = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    EventTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CorrelationId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    CausationId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    TechnicalAuditLogId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    OldValueJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NewValueJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MetadataJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Severity = table.Column<byte>(type: "tinyint", nullable: false),
                    IsSystemGenerated = table.Column<bool>(type: "bit", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppBusinessAuditLogs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BusinessAuditLogs_CorrelationId",
                table: "AppBusinessAuditLogs",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessAuditLogs_EntityId",
                table: "AppBusinessAuditLogs",
                column: "EntityId");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessAuditLogs_EntityType",
                table: "AppBusinessAuditLogs",
                column: "EntityType");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessAuditLogs_EventTime",
                table: "AppBusinessAuditLogs",
                column: "EventTime");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessAuditLogs_Module",
                table: "AppBusinessAuditLogs",
                column: "Module");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessAuditLogs_Severity",
                table: "AppBusinessAuditLogs",
                column: "Severity");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessAuditLogs_UserId",
                table: "AppBusinessAuditLogs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "UX_BusinessAuditLogs_EventId",
                table: "AppBusinessAuditLogs",
                column: "EventId",
                unique: true);

            migrationBuilder.Sql(
                """
                EXEC(N'
                    CREATE TRIGGER [TR_AppBusinessAuditLogs_Immutable]
                    ON [AppBusinessAuditLogs]
                    INSTEAD OF UPDATE, DELETE
                    AS
                    BEGIN
                        SET NOCOUNT ON;
                        THROW 51000, ''Business audit logs are immutable.'', 1;
                    END
                ')
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS [TR_AppBusinessAuditLogs_Immutable];");

            migrationBuilder.DropTable(
                name: "AppBusinessAuditLogs");
        }
    }
}
