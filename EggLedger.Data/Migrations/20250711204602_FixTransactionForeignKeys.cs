using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EggLedger.Data.Migrations
{
    /// <inheritdoc />
    public partial class FixTransactionForeignKeys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Transactions_Orders_OrderId1",
                table: "Transactions");

            migrationBuilder.DropForeignKey(
                name: "FK_Transactions_Users_PayerUserId",
                table: "Transactions");

            migrationBuilder.DropForeignKey(
                name: "FK_Transactions_Users_ReceiverUserId",
                table: "Transactions");

            migrationBuilder.DropIndex(
                name: "IX_Transactions_OrderId1",
                table: "Transactions");

            migrationBuilder.DropIndex(
                name: "IX_Transactions_PayerUserId",
                table: "Transactions");

            migrationBuilder.DropIndex(
                name: "IX_Transactions_ReceiverUserId",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "OrderId1",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "PayerUserId",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "ReceiverUserId",
                table: "Transactions");

            migrationBuilder.AddColumn<Guid>(
                name: "UserId",
                table: "Transactions",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_UserId",
                table: "Transactions",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Transactions_Users_UserId",
                table: "Transactions",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Transactions_Users_UserId",
                table: "Transactions");

            migrationBuilder.DropIndex(
                name: "IX_Transactions_UserId",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Transactions");

            migrationBuilder.AddColumn<Guid>(
                name: "OrderId1",
                table: "Transactions",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "PayerUserId",
                table: "Transactions",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "ReceiverUserId",
                table: "Transactions",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_OrderId1",
                table: "Transactions",
                column: "OrderId1");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_PayerUserId",
                table: "Transactions",
                column: "PayerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_ReceiverUserId",
                table: "Transactions",
                column: "ReceiverUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Transactions_Orders_OrderId1",
                table: "Transactions",
                column: "OrderId1",
                principalTable: "Orders",
                principalColumn: "OrderId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Transactions_Users_PayerUserId",
                table: "Transactions",
                column: "PayerUserId",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Transactions_Users_ReceiverUserId",
                table: "Transactions",
                column: "ReceiverUserId",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
