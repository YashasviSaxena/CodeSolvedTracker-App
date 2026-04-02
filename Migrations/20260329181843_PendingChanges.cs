using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CodeSolvedTracker.Migrations
{
    /// <inheritdoc />
    public partial class PendingChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Stats");

            migrationBuilder.DropColumn(
                name: "Easy",
                table: "Stats");

            migrationBuilder.DropColumn(
                name: "Hard",
                table: "Stats");

            migrationBuilder.DropColumn(
                name: "Medium",
                table: "Stats");

            migrationBuilder.DropColumn(
                name: "Platform",
                table: "Stats");

            migrationBuilder.RenameColumn(
                name: "Username",
                table: "Stats",
                newName: "Metric");

            migrationBuilder.RenameColumn(
                name: "TotalSolved",
                table: "Stats",
                newName: "Value");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Value",
                table: "Stats",
                newName: "TotalSolved");

            migrationBuilder.RenameColumn(
                name: "Metric",
                table: "Stats",
                newName: "Username");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Stats",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "Easy",
                table: "Stats",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Hard",
                table: "Stats",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Medium",
                table: "Stats",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Platform",
                table: "Stats",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }
    }
}
