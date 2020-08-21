using Microsoft.EntityFrameworkCore.Migrations;

namespace ParliamentVotes.Migrations
{
    public partial class QuestionUpdates : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "QuestionDescription",
                table: "Questions",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "Party_Id",
                table: "PartyVotes",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<int>(
                name: "Member_Id",
                table: "PartyVotes",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AlsoKnownAs",
                table: "Parties",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PartyVotes_Member_Id",
                table: "PartyVotes",
                column: "Member_Id");

            migrationBuilder.CreateIndex(
                name: "IX_PartyVotes_Party_Id",
                table: "PartyVotes",
                column: "Party_Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PartyVotes_Members_Member_Id",
                table: "PartyVotes",
                column: "Member_Id",
                principalTable: "Members",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PartyVotes_Parties_Party_Id",
                table: "PartyVotes",
                column: "Party_Id",
                principalTable: "Parties",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PartyVotes_Members_Member_Id",
                table: "PartyVotes");

            migrationBuilder.DropForeignKey(
                name: "FK_PartyVotes_Parties_Party_Id",
                table: "PartyVotes");

            migrationBuilder.DropIndex(
                name: "IX_PartyVotes_Member_Id",
                table: "PartyVotes");

            migrationBuilder.DropIndex(
                name: "IX_PartyVotes_Party_Id",
                table: "PartyVotes");

            migrationBuilder.DropColumn(
                name: "QuestionDescription",
                table: "Questions");

            migrationBuilder.DropColumn(
                name: "Member_Id",
                table: "PartyVotes");

            migrationBuilder.DropColumn(
                name: "AlsoKnownAs",
                table: "Parties");

            migrationBuilder.AlterColumn<int>(
                name: "Party_Id",
                table: "PartyVotes",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldNullable: true);
        }
    }
}
