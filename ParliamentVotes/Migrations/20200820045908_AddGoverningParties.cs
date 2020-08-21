using Microsoft.EntityFrameworkCore.Migrations;

namespace ParliamentVotes.Migrations
{
    public partial class AddGoverningParties : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GoverningParty",
                columns: table => new
                {
                    Session_Number = table.Column<int>(nullable: false),
                    Party_Id = table.Column<int>(nullable: false),
                    Relationship = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GoverningParty", x => new { x.Session_Number, x.Party_Id });
                    table.ForeignKey(
                        name: "FK_GoverningParty_Parties_Party_Id",
                        column: x => x.Party_Id,
                        principalTable: "Parties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GoverningParty_Sessions_Session_Number",
                        column: x => x.Session_Number,
                        principalTable: "Sessions",
                        principalColumn: "SessionNumber",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GoverningParty_Party_Id",
                table: "GoverningParty",
                column: "Party_Id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GoverningParty");
        }
    }
}
