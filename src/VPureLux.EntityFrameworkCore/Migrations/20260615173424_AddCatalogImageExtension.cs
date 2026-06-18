using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VPureLux.Migrations
{
    /// <inheritdoc />
    public partial class AddCatalogImageExtension : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ImageBase64",
                table: "AppProducts",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ImageFileName",
                table: "AppProducts",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ImageHash",
                table: "AppProducts",
                type: "char(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ImageMimeType",
                table: "AppProducts",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ImageBase64",
                table: "AppComponents",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ImageFileName",
                table: "AppComponents",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ImageHash",
                table: "AppComponents",
                type: "char(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ImageMimeType",
                table: "AppComponents",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImageBase64",
                table: "AppProducts");

            migrationBuilder.DropColumn(
                name: "ImageFileName",
                table: "AppProducts");

            migrationBuilder.DropColumn(
                name: "ImageHash",
                table: "AppProducts");

            migrationBuilder.DropColumn(
                name: "ImageMimeType",
                table: "AppProducts");

            migrationBuilder.DropColumn(
                name: "ImageBase64",
                table: "AppComponents");

            migrationBuilder.DropColumn(
                name: "ImageFileName",
                table: "AppComponents");

            migrationBuilder.DropColumn(
                name: "ImageHash",
                table: "AppComponents");

            migrationBuilder.DropColumn(
                name: "ImageMimeType",
                table: "AppComponents");
        }
    }
}
