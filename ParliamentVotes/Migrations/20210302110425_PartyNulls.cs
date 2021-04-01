using Microsoft.EntityFrameworkCore.Migrations;

namespace ParliamentVotes.Migrations
{
    public partial class PartyNulls : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Tenures_Parties_Party_Id",
                table: "Tenures");

            migrationBuilder.AlterColumn<int>(
                name: "Party_Id",
                table: "Tenures",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddForeignKey(
                name: "FK_Tenures_Parties_Party_Id",
                table: "Tenures",
                column: "Party_Id",
                principalTable: "Parties",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Tenures_Parties_Party_Id",
                table: "Tenures");

            migrationBuilder.AlterColumn<int>(
                name: "Party_Id",
                table: "Tenures",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Tenures_Parties_Party_Id",
                table: "Tenures",
                column: "Party_Id",
                principalTable: "Parties",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
