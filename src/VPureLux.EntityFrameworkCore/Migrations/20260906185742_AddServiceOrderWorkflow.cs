using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VPureLux.Migrations
{
    /// <inheritdoc />
    public partial class AddServiceOrderWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CancellationReason",
                table: "AppServiceOrders",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "StandardCostSnapshot",
                table: "AppServiceOrderLines",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CancellationReason",
                table: "AppServiceOrders");

            migrationBuilder.DropColumn(
                name: "StandardCostSnapshot",
                table: "AppServiceOrderLines");
        }
    }
}
