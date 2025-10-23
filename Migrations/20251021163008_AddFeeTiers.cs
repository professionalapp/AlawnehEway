using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AlawnehEway.Migrations
{
    /// <inheritdoc />
    public partial class AddFeeTiers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop the index only if it exists to avoid failures on databases that don't have it
            migrationBuilder.Sql(@"
IF EXISTS (
    SELECT 1
    FROM sys.indexes i
    WHERE i.name = 'IX_ExchangeRates_Country'
      AND i.object_id = OBJECT_ID(N'[dbo].[ExchangeRates]')
)
BEGIN
    DROP INDEX [IX_ExchangeRates_Country] ON [dbo].[ExchangeRates];
END
");

            migrationBuilder.AlterColumn<decimal>(
                name: "Rate",
                table: "ExchangeRates",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,4)",
                oldPrecision: 18,
                oldScale: 4);

            migrationBuilder.Sql(@"
IF NOT EXISTS (
    SELECT 1 FROM sys.objects 
    WHERE object_id = OBJECT_ID(N'[dbo].[FeeTiers]') AND type in (N'U')
)
BEGIN
    CREATE TABLE [dbo].[FeeTiers] (
        [Id] int NOT NULL IDENTITY,
        [Country] nvarchar(100) NOT NULL,
        [MinAmount] decimal(18,2) NOT NULL,
        [MaxAmount] decimal(18,2) NULL,
        [Fee] decimal(18,2) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [LastModifiedAt] datetime2 NULL,
        CONSTRAINT [PK_FeeTiers] PRIMARY KEY ([Id])
    );
END
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FeeTiers");

            migrationBuilder.AlterColumn<decimal>(
                name: "Rate",
                table: "ExchangeRates",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.CreateIndex(
                name: "IX_ExchangeRates_Country",
                table: "ExchangeRates",
                column: "Country",
                unique: true);
        }
    }
}
