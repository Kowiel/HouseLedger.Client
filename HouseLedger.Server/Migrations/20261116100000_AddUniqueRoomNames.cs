using HouseLedger.Server.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HouseLedger.Server.Migrations
{
    [DbContext(typeof(HouseLedgerDbContext))]
    [Migration("20261116100000_AddUniqueRoomNames")]
    public partial class AddUniqueRoomNames : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("CREATE UNIQUE INDEX \"IX_Rooms_UserId_NormalizedName\" ON \"Rooms\" (\"ApplicationUserId\", lower(btrim(\"Name\")))");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP INDEX \"IX_Rooms_UserId_NormalizedName\"");
        }
    }
}