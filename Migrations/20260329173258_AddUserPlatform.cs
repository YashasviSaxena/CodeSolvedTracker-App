using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CodeSolvedTracker.Migrations
{
    /// <inheritdoc />
    public partial class AddUserPlatform : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UserId",
                table: "UserPlatforms");

            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "UserPlatforms",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Email",
                table: "UserPlatforms");

            migrationBuilder.AddColumn<int>(
                name: "UserId",
                table: "UserPlatforms",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }
    }
}
