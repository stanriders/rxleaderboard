using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LazerRelaxLeaderboard.Migrations
{
    /// <inheritdoc />
    public partial class AddScoreBeatmapFK : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Scores_BeatmapId",
                table: "Scores",
                column: "BeatmapId");

            migrationBuilder.AddForeignKey(
                name: "FK_Scores_Beatmaps_BeatmapId",
                table: "Scores",
                column: "BeatmapId",
                principalTable: "Beatmaps",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Scores_Beatmaps_BeatmapId",
                table: "Scores");

            migrationBuilder.DropIndex(
                name: "IX_Scores_BeatmapId",
                table: "Scores");
        }
    }
}
