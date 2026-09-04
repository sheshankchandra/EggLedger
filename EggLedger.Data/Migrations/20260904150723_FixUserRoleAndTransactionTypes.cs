using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EggLedger.Data.Migrations;

/// <inheritdoc />
public partial class FixUserRoleAndTransactionTypes : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterColumn<decimal>(
            name: "Amount",
            table: "Transactions",
            type: "numeric(18,2)",
            nullable: false,
            oldClrType: typeof(int),
            oldType: "integer");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterColumn<int>(
            name: "Amount",
            table: "Transactions",
            type: "integer",
            nullable: false,
            oldClrType: typeof(decimal),
            oldType: "numeric(18,2)");
    }
}
