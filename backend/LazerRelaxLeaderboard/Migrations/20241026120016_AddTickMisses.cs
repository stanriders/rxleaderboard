using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LazerRelaxLeaderboard.Migrations
{
    /// <inheritdoc />
    public partial class AddTickMisses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "LegacySliderEndMisses",
                table: "Scores",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SliderTickMisses",
                table: "Scores",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LegacySliderEndMisses",
                table: "Scores");

            migrationBuilder.DropColumn(
                name: "SliderTickMisses",
                table: "Scores");
        }
    }
}
