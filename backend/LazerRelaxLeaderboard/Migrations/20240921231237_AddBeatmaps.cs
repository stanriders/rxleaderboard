using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace LazerRelaxLeaderboard.Migrations
{
    /// <inheritdoc />
    public partial class AddBeatmaps : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Beatmaps",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Artist = table.Column<string>(type: "text", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    CreatorId = table.Column<int>(type: "integer", nullable: false),
                    BeatmapSetId = table.Column<int>(type: "integer", nullable: false),
                    DifficultyName = table.Column<string>(type: "text", nullable: false),
                    ApproachRate = table.Column<double>(type: "double precision", nullable: false),
                    OverallDifficulty = table.Column<double>(type: "double precision", nullable: false),
                    CircleSize = table.Column<double>(type: "double precision", nullable: false),
                    HealthDrain = table.Column<double>(type: "double precision", nullable: false),
                    BeatsPerMinute = table.Column<double>(type: "double precision", nullable: false),
                    Circles = table.Column<int>(type: "integer", nullable: false),
                    Sliders = table.Column<int>(type: "integer", nullable: false),
                    Spinners = table.Column<int>(type: "integer", nullable: false),
                    StarRatingNormal = table.Column<double>(type: "double precision", nullable: false),
                    StarRating = table.Column<double>(type: "double precision", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Beatmaps", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Beatmaps");
        }
    }
}
