using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VPureLux.Migrations
{
    /// <inheritdoc />
    public partial class AddBomItemLineNo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "LineNo",
                table: "AppBomItems",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql("""
                WITH OrderedBomItems AS
                (
                    SELECT
                        [Id],
                        ROW_NUMBER() OVER (PARTITION BY [BomVersionId] ORDER BY [Id]) AS [RowNo]
                    FROM [AppBomItems]
                )
                UPDATE [AppBomItems]
                SET [LineNo] = (
                    SELECT [RowNo]
                    FROM OrderedBomItems
                    WHERE OrderedBomItems.[Id] = [AppBomItems].[Id]
                )
                """);

            migrationBuilder.CreateIndex(
                name: "IX_BomItems_BomVersionId_LineNo",
                table: "AppBomItems",
                columns: new[] { "BomVersionId", "LineNo" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_BomItems_BomVersionId_LineNo",
                table: "AppBomItems");

            migrationBuilder.DropColumn(
                name: "LineNo",
                table: "AppBomItems");
        }
    }
}
