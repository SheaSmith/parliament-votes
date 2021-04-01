using Microsoft.EntityFrameworkCore.Migrations;

namespace ParliamentVotes.Migrations
{
    public partial class SopTidyUp : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SupplementaryOrderPapers_Bills_AmendingBill_Id",
                table: "SupplementaryOrderPapers");

            migrationBuilder.DropForeignKey(
                name: "FK_SupplementaryOrderPapers_SupplementaryOrderPapers_AmendingSupplementaryOrderPaper_Id",
                table: "SupplementaryOrderPapers");

            migrationBuilder.DropForeignKey(
                name: "FK_SupplementaryOrderPapers_Parliaments_Parliament_Number",
                table: "SupplementaryOrderPapers");

            migrationBuilder.DropIndex(
                name: "IX_SupplementaryOrderPapers_AmendingSupplementaryOrderPaper_Id",
                table: "SupplementaryOrderPapers");

            migrationBuilder.DropIndex(
                name: "IX_SupplementaryOrderPapers_Parliament_Number",
                table: "SupplementaryOrderPapers");

            migrationBuilder.DropColumn(
                name: "AmendingSupplementaryOrderPaper_Id",
                table: "SupplementaryOrderPapers");

            migrationBuilder.DropColumn(
                name: "FileName",
                table: "SupplementaryOrderPapers");

            migrationBuilder.DropColumn(
                name: "Parliament_Number",
                table: "SupplementaryOrderPapers");

            migrationBuilder.DropColumn(
                name: "Slug",
                table: "SupplementaryOrderPapers");

            migrationBuilder.AlterColumn<int>(
                name: "AmendingBill_Id",
                table: "SupplementaryOrderPapers",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DirectoryUrl",
                table: "SupplementaryOrderPapers",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "Year",
                table: "SupplementaryOrderPapers",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddForeignKey(
                name: "FK_SupplementaryOrderPapers_Bills_AmendingBill_Id",
                table: "SupplementaryOrderPapers",
                column: "AmendingBill_Id",
                principalTable: "Bills",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SupplementaryOrderPapers_Bills_AmendingBill_Id",
                table: "SupplementaryOrderPapers");

            migrationBuilder.DropColumn(
                name: "DirectoryUrl",
                table: "SupplementaryOrderPapers");

            migrationBuilder.DropColumn(
                name: "Year",
                table: "SupplementaryOrderPapers");

            migrationBuilder.AlterColumn<int>(
                name: "AmendingBill_Id",
                table: "SupplementaryOrderPapers",
                type: "int",
                nullable: true,
                oldClrType: typeof(int));

            migrationBuilder.AddColumn<int>(
                name: "AmendingSupplementaryOrderPaper_Id",
                table: "SupplementaryOrderPapers",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FileName",
                table: "SupplementaryOrderPapers",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "Parliament_Number",
                table: "SupplementaryOrderPapers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Slug",
                table: "SupplementaryOrderPapers",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_SupplementaryOrderPapers_AmendingSupplementaryOrderPaper_Id",
                table: "SupplementaryOrderPapers",
                column: "AmendingSupplementaryOrderPaper_Id");

            migrationBuilder.CreateIndex(
                name: "IX_SupplementaryOrderPapers_Parliament_Number",
                table: "SupplementaryOrderPapers",
                column: "Parliament_Number");

            migrationBuilder.AddForeignKey(
                name: "FK_SupplementaryOrderPapers_Bills_AmendingBill_Id",
                table: "SupplementaryOrderPapers",
                column: "AmendingBill_Id",
                principalTable: "Bills",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SupplementaryOrderPapers_SupplementaryOrderPapers_AmendingSupplementaryOrderPaper_Id",
                table: "SupplementaryOrderPapers",
                column: "AmendingSupplementaryOrderPaper_Id",
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
    }
}
