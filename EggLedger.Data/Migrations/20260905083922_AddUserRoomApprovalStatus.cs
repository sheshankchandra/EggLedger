using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EggLedger.Data.Migrations;

/// <inheritdoc />
public partial class AddUserRoomApprovalStatus : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Default to Approved (2), not 0: every UserRoom row that already exists predates
        // this feature and was full membership, not a pending request. Defaulting to 0
        // (neither Pending=1 nor Approved=2) would silently lock every existing member out
        // of every room the next time RoomMemberHandler checks Status == Approved.
        migrationBuilder.AddColumn<int>(
            name: "Status",
            table: "UserRooms",
            type: "integer",
            nullable: false,
            defaultValue: 2);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "Status",
            table: "UserRooms");
    }
}
