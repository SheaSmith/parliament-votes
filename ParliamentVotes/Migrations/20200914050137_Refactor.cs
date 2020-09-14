using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace ParliamentVotes.Migrations
{
    public partial class Refactor : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Bills_Sessions_Session_Number",
                table: "Bills");

            migrationBuilder.DropForeignKey(
                name: "FK_GoverningParty_Sessions_Session_Number",
                table: "GoverningParty");

            migrationBuilder.DropForeignKey(
                name: "FK_SupplementaryOrderPapers_Sessions_Session_Number",
                table: "SupplementaryOrderPapers");

            migrationBuilder.DropTable(
                name: "Sessions");

            migrationBuilder.DropIndex(
                name: "IX_SupplementaryOrderPapers_Session_Number",
                table: "SupplementaryOrderPapers");

            migrationBuilder.DropPrimaryKey(
                name: "PK_GoverningParty",
                table: "GoverningParty");

            migrationBuilder.DropIndex(
                name: "IX_Bills_Session_Number",
                table: "Bills");

            migrationBuilder.DropColumn(
                name: "Session_Number",
                table: "SupplementaryOrderPapers");

            migrationBuilder.DropColumn(
                name: "QuestionDescription",
                table: "Questions");

            migrationBuilder.DropColumn(
                name: "QuestionSubtitle",
                table: "Questions");

            migrationBuilder.DropColumn(
                name: "QuestionTitle",
                table: "Questions");

            migrationBuilder.DropColumn(
                name: "Session_Number",
                table: "GoverningParty");

            migrationBuilder.DropColumn(
                name: "Session_Number",
                table: "Bills");

            migrationBuilder.AddColumn<int>(
                name: "Parliament_Number",
                table: "SupplementaryOrderPapers",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Bill_Id",
                table: "Questions",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Questions",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Member_Id",
                table: "Questions",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Parliament_Number",
                table: "Questions",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Subtitle",
                table: "Questions",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SupplementaryOrderPaper_Id",
                table: "Questions",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Title",
                table: "Questions",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "ImageUrl",
                table: "Members",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<string>(
                name: "ImageCopyright",
                table: "Members",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Parliament_Number",
                table: "GoverningParty",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Bills",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "Parliament_Number",
                table: "Bills",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddPrimaryKey(
                name: "PK_GoverningParty",
                table: "GoverningParty",
                columns: new[] { "Parliament_Number", "Party_Id" });

            migrationBuilder.CreateTable(
                name: "Parliaments",
                columns: table => new
                {
                    Number = table.Column<int>(nullable: false),
                    StartDate = table.Column<DateTime>(nullable: false),
                    EndDate = table.Column<DateTime>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Parliaments", x => x.Number);
                });

            migrationBuilder.CreateTable(
                name: "SeatingPlans",
                columns: table => new
                {
                    Parliament_Number = table.Column<int>(nullable: false),
                    Member_Id = table.Column<int>(nullable: false),
                    SeatIndex = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SeatingPlans", x => new { x.Parliament_Number, x.SeatIndex, x.Member_Id });
                    table.ForeignKey(
                        name: "FK_SeatingPlans_Members_Member_Id",
                        column: x => x.Member_Id,
                        principalTable: "Members",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SeatingPlans_Parliaments_Parliament_Number",
                        column: x => x.Parliament_Number,
                        principalTable: "Parliaments",
                        principalColumn: "Number",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SupplementaryOrderPapers_Parliament_Number",
                table: "SupplementaryOrderPapers",
                column: "Parliament_Number");

            migrationBuilder.CreateIndex(
                name: "IX_Questions_Bill_Id",
                table: "Questions",
                column: "Bill_Id");

            migrationBuilder.CreateIndex(
                name: "IX_Questions_Member_Id",
                table: "Questions",
                column: "Member_Id");

            migrationBuilder.CreateIndex(
                name: "IX_Questions_Parliament_Number",
                table: "Questions",
                column: "Parliament_Number");

            migrationBuilder.CreateIndex(
                name: "IX_Questions_SupplementaryOrderPaper_Id",
                table: "Questions",
                column: "SupplementaryOrderPaper_Id");

            migrationBuilder.CreateIndex(
                name: "IX_Bills_Parliament_Number",
                table: "Bills",
                column: "Parliament_Number");

            migrationBuilder.CreateIndex(
                name: "IX_SeatingPlans_Member_Id",
                table: "SeatingPlans",
                column: "Member_Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Bills_Parliaments_Parliament_Number",
                table: "Bills",
                column: "Parliament_Number",
                principalTable: "Parliaments",
                principalColumn: "Number",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_GoverningParty_Parliaments_Parliament_Number",
                table: "GoverningParty",
                column: "Parliament_Number",
                principalTable: "Parliaments",
                principalColumn: "Number",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Questions_Bills_Bill_Id",
                table: "Questions",
                column: "Bill_Id",
                principalTable: "Bills",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Questions_Members_Member_Id",
                table: "Questions",
                column: "Member_Id",
                principalTable: "Members",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Questions_Parliaments_Parliament_Number",
                table: "Questions",
                column: "Parliament_Number",
                principalTable: "Parliaments",
                principalColumn: "Number",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Questions_SupplementaryOrderPapers_SupplementaryOrderPaper_Id",
                table: "Questions",
                column: "SupplementaryOrderPaper_Id",
                principalTable: "SupplementaryOrderPapers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SupplementaryOrderPapers_Parliaments_Parliament_Number",
                table: "SupplementaryOrderPapers",
                column: "Parliament_Number",
                principalTable: "Parliaments",
                principalColumn: "Number",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Bills_Parliaments_Parliament_Number",
                table: "Bills");

            migrationBuilder.DropForeignKey(
                name: "FK_GoverningParty_Parliaments_Parliament_Number",
                table: "GoverningParty");

            migrationBuilder.DropForeignKey(
                name: "FK_Questions_Bills_Bill_Id",
                table: "Questions");

            migrationBuilder.DropForeignKey(
                name: "FK_Questions_Members_Member_Id",
                table: "Questions");

            migrationBuilder.DropForeignKey(
                name: "FK_Questions_Parliaments_Parliament_Number",
                table: "Questions");

            migrationBuilder.DropForeignKey(
                name: "FK_Questions_SupplementaryOrderPapers_SupplementaryOrderPaper_Id",
                table: "Questions");

            migrationBuilder.DropForeignKey(
                name: "FK_SupplementaryOrderPapers_Parliaments_Parliament_Number",
                table: "SupplementaryOrderPapers");

            migrationBuilder.DropTable(
                name: "SeatingPlans");

            migrationBuilder.DropTable(
                name: "Parliaments");

            migrationBuilder.DropIndex(
                name: "IX_SupplementaryOrderPapers_Parliament_Number",
                table: "SupplementaryOrderPapers");

            migrationBuilder.DropIndex(
                name: "IX_Questions_Bill_Id",
                table: "Questions");

            migrationBuilder.DropIndex(
                name: "IX_Questions_Member_Id",
                table: "Questions");

            migrationBuilder.DropIndex(
                name: "IX_Questions_Parliament_Number",
                table: "Questions");

            migrationBuilder.DropIndex(
                name: "IX_Questions_SupplementaryOrderPaper_Id",
                table: "Questions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_GoverningParty",
                table: "GoverningParty");

            migrationBuilder.DropIndex(
                name: "IX_Bills_Parliament_Number",
                table: "Bills");

            migrationBuilder.DropColumn(
                name: "Parliament_Number",
                table: "SupplementaryOrderPapers");

            migrationBuilder.DropColumn(
                name: "Bill_Id",
                table: "Questions");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "Questions");

            migrationBuilder.DropColumn(
                name: "Member_Id",
                table: "Questions");

            migrationBuilder.DropColumn(
                name: "Parliament_Number",
                table: "Questions");

            migrationBuilder.DropColumn(
                name: "Subtitle",
                table: "Questions");

            migrationBuilder.DropColumn(
                name: "SupplementaryOrderPaper_Id",
                table: "Questions");

            migrationBuilder.DropColumn(
                name: "Title",
                table: "Questions");

            migrationBuilder.DropColumn(
                name: "ImageCopyright",
                table: "Members");

            migrationBuilder.DropColumn(
                name: "Parliament_Number",
                table: "GoverningParty");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "Bills");

            migrationBuilder.DropColumn(
                name: "Parliament_Number",
                table: "Bills");

            migrationBuilder.AddColumn<int>(
                name: "Session_Number",
                table: "SupplementaryOrderPapers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "QuestionDescription",
                table: "Questions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "QuestionSubtitle",
                table: "Questions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "QuestionTitle",
                table: "Questions",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "ImageUrl",
                table: "Members",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Session_Number",
                table: "GoverningParty",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Session_Number",
                table: "Bills",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddPrimaryKey(
                name: "PK_GoverningParty",
                table: "GoverningParty",
                columns: new[] { "Session_Number", "Party_Id" });

            migrationBuilder.CreateTable(
                name: "Sessions",
                columns: table => new
                {
                    SessionNumber = table.Column<int>(type: "int", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sessions", x => x.SessionNumber);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SupplementaryOrderPapers_Session_Number",
                table: "SupplementaryOrderPapers",
                column: "Session_Number");

            migrationBuilder.CreateIndex(
                name: "IX_Bills_Session_Number",
                table: "Bills",
                column: "Session_Number");

            migrationBuilder.AddForeignKey(
                name: "FK_Bills_Sessions_Session_Number",
                table: "Bills",
                column: "Session_Number",
                principalTable: "Sessions",
                principalColumn: "SessionNumber",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_GoverningParty_Sessions_Session_Number",
                table: "GoverningParty",
                column: "Session_Number",
                principalTable: "Sessions",
                principalColumn: "SessionNumber",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_SupplementaryOrderPapers_Sessions_Session_Number",
                table: "SupplementaryOrderPapers",
                column: "Session_Number",
                principalTable: "Sessions",
                principalColumn: "SessionNumber",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
