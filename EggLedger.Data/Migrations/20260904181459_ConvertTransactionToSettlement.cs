using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EggLedger.Data.Migrations;

/// <inheritdoc />
public partial class ConvertTransactionToSettlement : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "Transactions");

        migrationBuilder.CreateTable(
            name: "Settlements",
            columns: table => new
            {
                SettlementId = table.Column<Guid>(type: "uuid", nullable: false),
                RoomId = table.Column<Guid>(type: "uuid", nullable: false),
                PayerId = table.Column<Guid>(type: "uuid", nullable: false),
                ReceiverId = table.Column<Guid>(type: "uuid", nullable: false),
                Amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                Datestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                Note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Settlements", x => x.SettlementId);
                table.ForeignKey(
                    name: "FK_Settlement_Payer",
                    column: x => x.PayerId,
                    principalTable: "Users",
                    principalColumn: "UserId",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_Settlement_Receiver",
                    column: x => x.ReceiverId,
                    principalTable: "Users",
                    principalColumn: "UserId",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_Settlements_Rooms_RoomId",
                    column: x => x.RoomId,
                    principalTable: "Rooms",
                    principalColumn: "RoomId",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_Settlements_PayerId",
            table: "Settlements",
            column: "PayerId");

        migrationBuilder.CreateIndex(
            name: "IX_Settlements_ReceiverId",
            table: "Settlements",
            column: "ReceiverId");

        migrationBuilder.CreateIndex(
            name: "IX_Settlements_RoomId",
            table: "Settlements",
            column: "RoomId");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "Settlements");

        migrationBuilder.CreateTable(
            name: "Transactions",
            columns: table => new
            {
                TransactionId = table.Column<Guid>(type: "uuid", nullable: false),
                OrderId = table.Column<Guid>(type: "uuid", nullable: false),
                PayerId = table.Column<Guid>(type: "uuid", nullable: false),
                ReceiverId = table.Column<Guid>(type: "uuid", nullable: false),
                Amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                Datestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                Status = table.Column<int>(type: "integer", nullable: false),
                UserId = table.Column<Guid>(type: "uuid", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Transactions", x => x.TransactionId);
                table.ForeignKey(
                    name: "FK_Transaction_Payer",
                    column: x => x.PayerId,
                    principalTable: "Users",
                    principalColumn: "UserId",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_Transaction_Receiver",
                    column: x => x.ReceiverId,
                    principalTable: "Users",
                    principalColumn: "UserId",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_Transactions_Orders_OrderId",
                    column: x => x.OrderId,
                    principalTable: "Orders",
                    principalColumn: "OrderId",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_Transactions_Users_UserId",
                    column: x => x.UserId,
                    principalTable: "Users",
                    principalColumn: "UserId");
            });

        migrationBuilder.CreateIndex(
            name: "IX_Transactions_OrderId",
            table: "Transactions",
            column: "OrderId");

        migrationBuilder.CreateIndex(
            name: "IX_Transactions_PayerId",
            table: "Transactions",
            column: "PayerId");

        migrationBuilder.CreateIndex(
            name: "IX_Transactions_ReceiverId",
            table: "Transactions",
            column: "ReceiverId");

        migrationBuilder.CreateIndex(
            name: "IX_Transactions_UserId",
            table: "Transactions",
            column: "UserId");
    }
}
