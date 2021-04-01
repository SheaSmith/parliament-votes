using Microsoft.EntityFrameworkCore.Migrations;

namespace ParliamentVotes.Migrations
{
    public partial class OptionalSopBill : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SupplementaryOrderPapers_Bills_AmendingBill_Id",
                table: "SupplementaryOrderPapers");

            migrationBuilder.AlterColumn<int>(
                name: "AmendingBill_Id",
                table: "SupplementaryOrderPapers",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddForeignKey(
                name: "FK_SupplementaryOrderPapers_Bills_AmendingBill_Id",
                table: "SupplementaryOrderPapers",
                column: "AmendingBill_Id",
                principalTable: "Bills",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SupplementaryOrderPapers_Bills_AmendingBill_Id",
                table: "SupplementaryOrderPapers");

            migrationBuilder.AlterColumn<int>(
                name: "AmendingBill_Id",
                table: "SupplementaryOrderPapers",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_SupplementaryOrderPapers_Bills_AmendingBill_Id",
                table: "SupplementaryOrderPapers",
                column: "AmendingBill_Id",
                principalTable: "Bills",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
