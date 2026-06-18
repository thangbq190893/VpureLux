using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VPureLux.Migrations
{
    /// <inheritdoc />
    public partial class EnforceSinglePublishedBomPerProduct : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "UX_BomVersions_ProductId_Published",
                table: "AppBomVersions",
                column: "ProductId",
                unique: true,
                filter: "[Status] = 2 AND [IsDeleted] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_BomVersions_ProductId_Published",
                table: "AppBomVersions");
        }
    }
}
