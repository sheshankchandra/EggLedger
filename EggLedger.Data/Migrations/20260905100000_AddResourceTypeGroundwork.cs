using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EggLedger.Data.Migrations;

/// <inheritdoc />
public partial class AddResourceTypeGroundwork : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<Guid>(
            name: "ResourceTypeId",
            table: "Containers",
            type: "uuid",
            nullable: false,
            defaultValue: new Guid("11111111-1111-1111-1111-111111111111"));

        migrationBuilder.CreateTable(
            name: "ResourceTypes",
            columns: table => new
            {
                ResourceTypeId = table.Column<Guid>(type: "uuid", nullable: false),
                Name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                DisplayName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                Singular = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                Plural = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                InventorySingular = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                InventoryPlural = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                Icon = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                IsActive = table.Column<bool>(type: "boolean", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ResourceTypes", x => x.ResourceTypeId);
            });

        migrationBuilder.InsertData(
            table: "ResourceTypes",
            columns: new[] { "ResourceTypeId", "CreatedAt", "DisplayName", "Icon", "InventoryPlural", "InventorySingular", "IsActive", "Name", "Plural", "Singular" },
            values: new object[] { new Guid("11111111-1111-1111-1111-111111111111"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Eggs", "🥚", "batches", "batch", true, "eggs", "eggs", "egg" });

        migrationBuilder.CreateIndex(
            name: "IX_Containers_ResourceTypeId",
            table: "Containers",
            column: "ResourceTypeId");

        migrationBuilder.CreateIndex(
            name: "IX_ResourceTypes_Name",
            table: "ResourceTypes",
            column: "Name",
            unique: true);

        migrationBuilder.AddForeignKey(
            name: "FK_Containers_ResourceTypes_ResourceTypeId",
            table: "Containers",
            column: "ResourceTypeId",
            principalTable: "ResourceTypes",
            principalColumn: "ResourceTypeId",
            onDelete: ReferentialAction.Restrict);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_Containers_ResourceTypes_ResourceTypeId",
            table: "Containers");

        migrationBuilder.DropTable(
            name: "ResourceTypes");

        migrationBuilder.DropIndex(
            name: "IX_Containers_ResourceTypeId",
            table: "Containers");

        migrationBuilder.DropColumn(
            name: "ResourceTypeId",
            table: "Containers");
    }
}
