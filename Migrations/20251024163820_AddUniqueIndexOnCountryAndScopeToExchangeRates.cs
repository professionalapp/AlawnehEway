using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AlawnehEway.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueIndexOnCountryAndScopeToExchangeRates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_ExchangeRates_Country_Scope",
                table: "ExchangeRates",
                columns: new[] { "Country", "Scope" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ExchangeRates_Country_Scope",
                table: "ExchangeRates");
        }
    }
}
