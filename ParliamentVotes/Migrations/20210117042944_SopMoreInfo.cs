using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace ParliamentVotes.Migrations
{
    public partial class SopMoreInfo : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "Date",
                table: "SupplementaryOrderPapers",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "Parliament_Number",
                table: "SupplementaryOrderPapers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_SupplementaryOrderPapers_Parliament_Number",
                table: "SupplementaryOrderPapers",
                column: "Parliament_Number");

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
                name: "FK_SupplementaryOrderPapers_Parliaments_Parliament_Number",
                table: "SupplementaryOrderPapers");

            migrationBuilder.DropIndex(
                name: "IX_SupplementaryOrderPapers_Parliament_Number",
                table: "SupplementaryOrderPapers");

            migrationBuilder.DropColumn(
                name: "Date",
                table: "SupplementaryOrderPapers");

            migrationBuilder.DropColumn(
                name: "Parliament_Number",
                table: "SupplementaryOrderPapers");
        }
    }
}
