using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AlawnehEway.Migrations
{
    /// <inheritdoc />
    public partial class AddReferenceToCurrencyExchange : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Reference",
                table: "CurrencyExchanges",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Reference",
                table: "CurrencyExchanges");
        }
    }
}
