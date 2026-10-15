using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProductFlow.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMinimumStockLevel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MinimumStockLevel",
                table: "Products",
                type: "int",
                nullable: false,
                defaultValue: 5);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MinimumStockLevel",
                table: "Products");
        }
    }
}
