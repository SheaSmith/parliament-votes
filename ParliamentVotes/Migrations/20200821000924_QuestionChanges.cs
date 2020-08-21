using Microsoft.EntityFrameworkCore.Migrations;

namespace ParliamentVotes.Migrations
{
    public partial class QuestionChanges : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BillTitle",
                table: "Questions");

            migrationBuilder.DropColumn(
                name: "BillUrl",
                table: "Questions");

            migrationBuilder.AddColumn<string>(
                name: "QuestionSubtitle",
                table: "Questions",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "QuestionSubtitle",
                table: "Questions");

            migrationBuilder.AddColumn<string>(
                name: "BillTitle",
                table: "Questions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BillUrl",
                table: "Questions",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
