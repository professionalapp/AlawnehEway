using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AlawnehEway.Migrations
{
    /// <inheritdoc />
    public partial class AddRemittanceReferenceAndStatusChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "PaidAt",
                table: "Remittances",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Reference",
                table: "Remittances",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "Remittances",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "RemittanceChangeRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RemittanceId = table.Column<int>(type: "int", nullable: false),
                    RequestType = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ProposedCountry = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ProposedAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    ProposedReason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ProposedPurpose = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ApprovedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RemittanceChangeRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RemittanceChangeRequests_Remittances_RemittanceId",
                        column: x => x.RemittanceId,
                        principalTable: "Remittances",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Remittances_Reference",
                table: "Remittances",
                column: "Reference");

            migrationBuilder.CreateIndex(
                name: "IX_RemittanceChangeRequests_RemittanceId",
                table: "RemittanceChangeRequests",
                column: "RemittanceId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RemittanceChangeRequests");

            migrationBuilder.DropIndex(
                name: "IX_Remittances_Reference",
                table: "Remittances");

            migrationBuilder.DropColumn(
                name: "PaidAt",
                table: "Remittances");

            migrationBuilder.DropColumn(
                name: "Reference",
                table: "Remittances");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Remittances");
        }
    }
}
