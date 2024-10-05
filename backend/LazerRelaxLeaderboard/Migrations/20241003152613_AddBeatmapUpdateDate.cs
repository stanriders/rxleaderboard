using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LazerRelaxLeaderboard.Migrations
{
    /// <inheritdoc />
    public partial class AddBeatmapUpdateDate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ScoresUpdatedOn",
                table: "Beatmaps",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: DateTime.UtcNow);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ScoresUpdatedOn",
                table: "Beatmaps");
        }
    }
}
