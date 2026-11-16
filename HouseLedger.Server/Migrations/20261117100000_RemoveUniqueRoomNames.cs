using HouseLedger.Server.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HouseLedger.Server.Migrations
{
    [DbContext(typeof(HouseLedgerDbContext))]
    [Migration("20261117100000_RemoveUniqueRoomNames")]
    public partial class RemoveUniqueRoomNames : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Rooms_UserId_NormalizedName",
                table: "Rooms");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("CREATE UNIQUE INDEX \"IX_Rooms_UserId_NormalizedName\" ON \"Rooms\" (\"ApplicationUserId\", lower(btrim(\"Name\")))");
        }
    }
}