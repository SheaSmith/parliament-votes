using Microsoft.EntityFrameworkCore.Migrations;

namespace ParliamentVotes.Migrations
{
    public partial class Fluent : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SplitPartyVotes");

            migrationBuilder.CreateTable(
                name: "MemberPartyVote",
                columns: table => new
                {
                    SplitPartyVotesId = table.Column<int>(type: "int", nullable: false),
                    SplitPartyVotesId1 = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MemberPartyVote", x => new { x.SplitPartyVotesId, x.SplitPartyVotesId1 });
                    table.ForeignKey(
                        name: "FK_MemberPartyVote_Members_SplitPartyVotesId1",
                        column: x => x.SplitPartyVotesId1,
                        principalTable: "Members",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MemberPartyVote_PartyVotes_SplitPartyVotesId",
                        column: x => x.SplitPartyVotesId,
                        principalTable: "PartyVotes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MemberPartyVote_SplitPartyVotesId1",
                table: "MemberPartyVote",
                column: "SplitPartyVotesId1");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MemberPartyVote");

            migrationBuilder.CreateTable(
                name: "SplitPartyVotes",
                columns: table => new
                {
                    PartyVote_Id = table.Column<int>(type: "int", nullable: false),
                    Member_Id = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SplitPartyVotes", x => new { x.PartyVote_Id, x.Member_Id });
                    table.ForeignKey(
                        name: "FK_SplitPartyVotes_Members_Member_Id",
                        column: x => x.Member_Id,
                        principalTable: "Members",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SplitPartyVotes_PartyVotes_PartyVote_Id",
                        column: x => x.PartyVote_Id,
                        principalTable: "PartyVotes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SplitPartyVotes_Member_Id",
                table: "SplitPartyVotes",
                column: "Member_Id");
        }
    }
}
