using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace ParliamentVotes.Migrations
{
    public partial class Viewpoint : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "PersonalVoteConservativeViewPoint",
                table: "Questions",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Acts",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(nullable: false),
                    LastUpdated = table.Column<DateTime>(nullable: false),
                    ActType = table.Column<int>(nullable: false),
                    Slug = table.Column<string>(nullable: false),
                    FileName = table.Column<string>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Acts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Bills",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(nullable: false),
                    LastUpdated = table.Column<DateTime>(nullable: false),
                    Session_Number = table.Column<int>(nullable: false),
                    Member_Id = table.Column<int>(nullable: false),
                    Type = table.Column<int>(nullable: false),
                    Slug = table.Column<string>(nullable: false),
                    FileName = table.Column<string>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bills", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Bills_Members_Member_Id",
                        column: x => x.Member_Id,
                        principalTable: "Members",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Bills_Sessions_Session_Number",
                        column: x => x.Session_Number,
                        principalTable: "Sessions",
                        principalColumn: "SessionNumber",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SupplementaryOrderPapers",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Number = table.Column<int>(nullable: false),
                    LastUpdated = table.Column<DateTime>(nullable: false),
                    Session_Number = table.Column<int>(nullable: false),
                    Member_Id = table.Column<int>(nullable: false),
                    AmendingBill_Id = table.Column<int>(nullable: true),
                    AmendingSupplementaryOrderPaper_Id = table.Column<int>(nullable: true),
                    Type = table.Column<int>(nullable: false),
                    Slug = table.Column<string>(nullable: false),
                    FileName = table.Column<string>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupplementaryOrderPapers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SupplementaryOrderPapers_Bills_AmendingBill_Id",
                        column: x => x.AmendingBill_Id,
                        principalTable: "Bills",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SupplementaryOrderPapers_SupplementaryOrderPapers_AmendingSupplementaryOrderPaper_Id",
                        column: x => x.AmendingSupplementaryOrderPaper_Id,
                        principalTable: "SupplementaryOrderPapers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SupplementaryOrderPapers_Members_Member_Id",
                        column: x => x.Member_Id,
                        principalTable: "Members",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SupplementaryOrderPapers_Sessions_Session_Number",
                        column: x => x.Session_Number,
                        principalTable: "Sessions",
                        principalColumn: "SessionNumber",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Bills_Member_Id",
                table: "Bills",
                column: "Member_Id");

            migrationBuilder.CreateIndex(
                name: "IX_Bills_Session_Number",
                table: "Bills",
                column: "Session_Number");

            migrationBuilder.CreateIndex(
                name: "IX_SupplementaryOrderPapers_AmendingBill_Id",
                table: "SupplementaryOrderPapers",
                column: "AmendingBill_Id");

            migrationBuilder.CreateIndex(
                name: "IX_SupplementaryOrderPapers_AmendingSupplementaryOrderPaper_Id",
                table: "SupplementaryOrderPapers",
                column: "AmendingSupplementaryOrderPaper_Id");

            migrationBuilder.CreateIndex(
                name: "IX_SupplementaryOrderPapers_Member_Id",
                table: "SupplementaryOrderPapers",
                column: "Member_Id");

            migrationBuilder.CreateIndex(
                name: "IX_SupplementaryOrderPapers_Session_Number",
                table: "SupplementaryOrderPapers",
                column: "Session_Number");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Acts");

            migrationBuilder.DropTable(
                name: "SupplementaryOrderPapers");

            migrationBuilder.DropTable(
                name: "Bills");

            migrationBuilder.DropColumn(
                name: "PersonalVoteConservativeViewPoint",
                table: "Questions");
        }
    }
}
