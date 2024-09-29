using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LazerRelaxLeaderboard.Migrations
{
    /// <inheritdoc />
    public partial class AddPpIndices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "BeatmapId",
                table: "Scores",
                type: "integer",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.CreateIndex(
                name: "IX_Users_TotalPp",
                table: "Users",
                column: "TotalPp");

            migrationBuilder.CreateIndex(
                name: "IX_Scores_Pp",
                table: "Scores",
                column: "Pp");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Users_TotalPp",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Scores_Pp",
                table: "Scores");

            migrationBuilder.AlterColumn<long>(
                name: "BeatmapId",
                table: "Scores",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");
        }
    }
}
