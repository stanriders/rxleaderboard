using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LazerRelaxLeaderboard.Migrations
{
    /// <inheritdoc />
    public partial class AddMoreData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "LegacySliderEnds",
                table: "Scores",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SliderEnds",
                table: "Scores",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SliderTicks",
                table: "Scores",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SpinnerBonus",
                table: "Scores",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SpinnerSpins",
                table: "Scores",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MaxCombo",
                table: "Beatmaps",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Beatmaps",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LegacySliderEnds",
                table: "Scores");

            migrationBuilder.DropColumn(
                name: "SliderEnds",
                table: "Scores");

            migrationBuilder.DropColumn(
                name: "SliderTicks",
                table: "Scores");

            migrationBuilder.DropColumn(
                name: "SpinnerBonus",
                table: "Scores");

            migrationBuilder.DropColumn(
                name: "SpinnerSpins",
                table: "Scores");

            migrationBuilder.DropColumn(
                name: "MaxCombo",
                table: "Beatmaps");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Beatmaps");
        }
    }
}
