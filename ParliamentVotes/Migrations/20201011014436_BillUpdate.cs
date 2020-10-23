using Microsoft.EntityFrameworkCore.Migrations;

namespace ParliamentVotes.Migrations
{
    public partial class BillUpdate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Bills_Parliaments_Parliament_Number",
                table: "Bills");

            migrationBuilder.DropIndex(
                name: "IX_Bills_Parliament_Number",
                table: "Bills");

            migrationBuilder.DropColumn(
                name: "FileName",
                table: "Bills");

            migrationBuilder.DropColumn(
                name: "Parliament_Number",
                table: "Bills");

            migrationBuilder.DropColumn(
                name: "Slug",
                table: "Bills");

            migrationBuilder.AddColumn<string>(
                name: "BillNumber",
                table: "Bills",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DirectoryUrl",
                table: "Bills",
                nullable: false,
                defaultValue: "");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BillNumber",
                table: "Bills");

            migrationBuilder.DropColumn(
                name: "DirectoryUrl",
                table: "Bills");

            migrationBuilder.AddColumn<string>(
                name: "FileName",
                table: "Bills",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "Parliament_Number",
                table: "Bills",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Slug",
                table: "Bills",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Bills_Parliament_Number",
                table: "Bills",
                column: "Parliament_Number");

            migrationBuilder.AddForeignKey(
                name: "FK_Bills_Parliaments_Parliament_Number",
                table: "Bills",
                column: "Parliament_Number",
                principalTable: "Parliaments",
                principalColumn: "Number",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
