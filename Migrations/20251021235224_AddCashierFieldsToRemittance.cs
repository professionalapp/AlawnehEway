using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AlawnehEway.Migrations
{
    /// <inheritdoc />
    public partial class AddCashierFieldsToRemittance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ReceiverUserId",
                table: "Remittances",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SenderUserId",
                table: "Remittances",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Remittances_ReceiverUserId",
                table: "Remittances",
                column: "ReceiverUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Remittances_SenderUserId",
                table: "Remittances",
                column: "SenderUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Remittances_Users_ReceiverUserId",
                table: "Remittances",
                column: "ReceiverUserId",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Remittances_Users_SenderUserId",
                table: "Remittances",
                column: "SenderUserId",
                principalTable: "Users",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Remittances_Users_ReceiverUserId",
                table: "Remittances");

            migrationBuilder.DropForeignKey(
                name: "FK_Remittances_Users_SenderUserId",
                table: "Remittances");

            migrationBuilder.DropIndex(
                name: "IX_Remittances_ReceiverUserId",
                table: "Remittances");

            migrationBuilder.DropIndex(
                name: "IX_Remittances_SenderUserId",
                table: "Remittances");

            migrationBuilder.DropColumn(
                name: "ReceiverUserId",
                table: "Remittances");

            migrationBuilder.DropColumn(
                name: "SenderUserId",
                table: "Remittances");
        }
    }
}
