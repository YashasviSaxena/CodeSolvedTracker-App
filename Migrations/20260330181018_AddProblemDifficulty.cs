using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CodeSolvedTracker.Migrations
{
    /// <inheritdoc />
    public partial class AddProblemDifficulty : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UserId",
                table: "UserPlatforms");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "Problems");

            migrationBuilder.RenameColumn(
                name: "TotalPoints",
                table: "Stats",
                newName: "TotalSolved");

            migrationBuilder.RenameColumn(
                name: "SolvedProblems",
                table: "Stats",
                newName: "TotalProblems");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "TotalSolved",
                table: "Stats",
                newName: "TotalPoints");

            migrationBuilder.RenameColumn(
                name: "TotalProblems",
                table: "Stats",
                newName: "SolvedProblems");

            migrationBuilder.AddColumn<int>(
                name: "UserId",
                table: "UserPlatforms",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Problems",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }
    }
}
