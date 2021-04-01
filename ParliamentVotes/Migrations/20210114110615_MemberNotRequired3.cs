using Microsoft.EntityFrameworkCore.Migrations;

namespace ParliamentVotes.Migrations
{
    public partial class MemberNotRequired3 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Bills_Members_Member_Id",
                table: "Bills");

            migrationBuilder.AlterColumn<int>(
                name: "Member_Id",
                table: "Bills",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddForeignKey(
                name: "FK_Bills_Members_Member_Id",
                table: "Bills",
                column: "Member_Id",
                principalTable: "Members",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Bills_Members_Member_Id",
                table: "Bills");

            migrationBuilder.AlterColumn<int>(
                name: "Member_Id",
                table: "Bills",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Bills_Members_Member_Id",
                table: "Bills",
                column: "Member_Id",
                principalTable: "Members",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
