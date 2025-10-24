using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AlawnehEway.Migrations
{
    /// <inheritdoc />
    public partial class RemoveOldCountryIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ExchangeRates_Country",
                table: "ExchangeRates");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_ExchangeRates_Country",
                table: "ExchangeRates",
                column: "Country",
                unique: true);
        }
    }
}
