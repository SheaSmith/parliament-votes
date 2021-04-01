using Microsoft.EntityFrameworkCore.Migrations;

namespace ParliamentVotes.Migrations
{
    public partial class BillParliamentMultiMember : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Bills_Members_Member_Id",
                table: "Bills");

            migrationBuilder.DropIndex(
                name: "IX_Bills_Member_Id",
                table: "Bills");

            migrationBuilder.DropColumn(
                name: "Member_Id",
                table: "Bills");

            migrationBuilder.CreateTable(
                name: "BillMember",
                columns: table => new
                {
                    BillsId = table.Column<int>(type: "int", nullable: false),
                    MembersId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BillMember", x => new { x.BillsId, x.MembersId });
                    table.ForeignKey(
                        name: "FK_BillMember_Bills_BillsId",
                        column: x => x.BillsId,
                        principalTable: "Bills",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BillMember_Members_MembersId",
                        column: x => x.MembersId,
                        principalTable: "Members",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BillParliament",
                columns: table => new
                {
                    BillsId = table.Column<int>(type: "int", nullable: false),
                    ParliamentsNumber = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BillParliament", x => new { x.BillsId, x.ParliamentsNumber });
                    table.ForeignKey(
                        name: "FK_BillParliament_Bills_BillsId",
                        column: x => x.BillsId,
                        principalTable: "Bills",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BillParliament_Parliaments_ParliamentsNumber",
                        column: x => x.ParliamentsNumber,
                        principalTable: "Parliaments",
                        principalColumn: "Number",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BillMember_MembersId",
                table: "BillMember",
                column: "MembersId");

            migrationBuilder.CreateIndex(
                name: "IX_BillParliament_ParliamentsNumber",
                table: "BillParliament",
                column: "ParliamentsNumber");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BillMember");

            migrationBuilder.DropTable(
                name: "BillParliament");

            migrationBuilder.AddColumn<int>(
                name: "Member_Id",
                table: "Bills",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Bills_Member_Id",
                table: "Bills",
                column: "Member_Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Bills_Members_Member_Id",
                table: "Bills",
                column: "Member_Id",
                principalTable: "Members",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
