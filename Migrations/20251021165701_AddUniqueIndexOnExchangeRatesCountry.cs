using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AlawnehEway.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueIndexOnExchangeRatesCountry : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
-- Normalize country names to canonical English values
UPDATE [dbo].[ExchangeRates] SET Country = 'Jordan' WHERE Country IN (N'الأردن');
UPDATE [dbo].[ExchangeRates] SET Country = 'Saudi Arabia' WHERE Country IN (N'السعودية', N'KSA');
UPDATE [dbo].[ExchangeRates] SET Country = 'UAE' WHERE Country IN (N'الإمارات', N'United Arab Emirates');
UPDATE [dbo].[ExchangeRates] SET Country = 'Turkey' WHERE Country IN (N'تركيا');
UPDATE [dbo].[ExchangeRates] SET Country = 'Egypt' WHERE Country IN (N'مصر');

-- Remove duplicates, keep the most recently modified/created per country
;WITH Ranked AS (
    SELECT *, ROW_NUMBER() OVER (PARTITION BY Country ORDER BY ISNULL(LastModifiedAt, CreatedAt) DESC, Id DESC) AS rn
    FROM [dbo].[ExchangeRates]
)
DELETE FROM Ranked WHERE rn > 1;

-- Create unique index if missing
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes WHERE name = 'IX_ExchangeRates_Country' AND object_id = OBJECT_ID('[dbo].[ExchangeRates]')
)
BEGIN
    CREATE UNIQUE INDEX [IX_ExchangeRates_Country] ON [dbo].[ExchangeRates]([Country]);
END
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF EXISTS (
    SELECT 1 FROM sys.indexes WHERE name = 'IX_ExchangeRates_Country' AND object_id = OBJECT_ID('[dbo].[ExchangeRates]')
)
BEGIN
    DROP INDEX [IX_ExchangeRates_Country] ON [dbo].[ExchangeRates];
END
");
        }
    }
}
