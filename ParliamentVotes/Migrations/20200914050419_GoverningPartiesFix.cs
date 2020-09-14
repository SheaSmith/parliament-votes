using Microsoft.EntityFrameworkCore.Migrations;

namespace ParliamentVotes.Migrations
{
    public partial class GoverningPartiesFix : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_GoverningParty_Parliaments_Parliament_Number",
                table: "GoverningParty");

            migrationBuilder.DropForeignKey(
                name: "FK_GoverningParty_Parties_Party_Id",
                table: "GoverningParty");

            migrationBuilder.DropPrimaryKey(
                name: "PK_GoverningParty",
                table: "GoverningParty");

            migrationBuilder.RenameTable(
                name: "GoverningParty",
                newName: "GoverningParties");

            migrationBuilder.RenameIndex(
                name: "IX_GoverningParty_Party_Id",
                table: "GoverningParties",
                newName: "IX_GoverningParties_Party_Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_GoverningParties",
                table: "GoverningParties",
                columns: new[] { "Parliament_Number", "Party_Id" });

            migrationBuilder.AddForeignKey(
                name: "FK_GoverningParties_Parliaments_Parliament_Number",
                table: "GoverningParties",
                column: "Parliament_Number",
                principalTable: "Parliaments",
                principalColumn: "Number",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_GoverningParties_Parties_Party_Id",
                table: "GoverningParties",
                column: "Party_Id",
                principalTable: "Parties",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_GoverningParties_Parliaments_Parliament_Number",
                table: "GoverningParties");

            migrationBuilder.DropForeignKey(
                name: "FK_GoverningParties_Parties_Party_Id",
                table: "GoverningParties");

            migrationBuilder.DropPrimaryKey(
                name: "PK_GoverningParties",
                table: "GoverningParties");

            migrationBuilder.RenameTable(
                name: "GoverningParties",
                newName: "GoverningParty");

            migrationBuilder.RenameIndex(
                name: "IX_GoverningParties_Party_Id",
                table: "GoverningParty",
                newName: "IX_GoverningParty_Party_Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_GoverningParty",
                table: "GoverningParty",
                columns: new[] { "Parliament_Number", "Party_Id" });

            migrationBuilder.AddForeignKey(
                name: "FK_GoverningParty_Parliaments_Parliament_Number",
                table: "GoverningParty",
                column: "Parliament_Number",
                principalTable: "Parliaments",
                principalColumn: "Number",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_GoverningParty_Parties_Party_Id",
                table: "GoverningParty",
                column: "Party_Id",
                principalTable: "Parties",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
