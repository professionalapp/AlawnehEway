using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AlawnehEway.Migrations
{
    /// <inheritdoc />
    public partial class AddExchangeRateScope : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Scope",
                table: "ExchangeRates",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Scope",
                table: "ExchangeRates");
        }
    }
}
