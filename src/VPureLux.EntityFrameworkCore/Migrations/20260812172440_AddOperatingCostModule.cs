using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VPureLux.Migrations
{
    /// <inheritdoc />
    public partial class AddOperatingCostModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AppOperatingCostCategories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Direction = table.Column<byte>(type: "tinyint", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    ExtraProperties = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeleterId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeletionTime = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppOperatingCostCategories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppOperatingCostEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EntryDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Direction = table.Column<byte>(type: "tinyint", nullable: false),
                    CategoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CategoryNameSnapshot = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PaymentStatus = table.Column<byte>(type: "tinyint", nullable: false),
                    DueDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PaymentDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Counterparty = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ReferenceNo = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    Note = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    ExtraProperties = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeleterId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeletionTime = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppOperatingCostEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppOperatingCostEntries_AppOperatingCostCategories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "AppOperatingCostCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "UX_OperatingCostCategories_Code",
                table: "AppOperatingCostCategories",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OperatingCostEntries_CategoryId",
                table: "AppOperatingCostEntries",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_OperatingCostEntries_Direction_PaymentStatus",
                table: "AppOperatingCostEntries",
                columns: new[] { "Direction", "PaymentStatus" });

            migrationBuilder.CreateIndex(
                name: "IX_OperatingCostEntries_EntryDate",
                table: "AppOperatingCostEntries",
                column: "EntryDate");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppOperatingCostEntries");

            migrationBuilder.DropTable(
                name: "AppOperatingCostCategories");
        }
    }
}
