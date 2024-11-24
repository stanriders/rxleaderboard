using System.Text.Json.Serialization;
using LazerRelaxLeaderboard.OsuApi.Models;

namespace LazerRelaxLeaderboard.Contracts
{
    public class BeatmapsResponse
    {
        public List<ListingBeatmap> Beatmaps { get; set; } = new();
        public int Total { get; set; }
    }

    public class ListingBeatmap
    {
        public required int Id { get; set; }

        public required string Artist { get; set; } = null!;

        public required string Title { get; set; } = null!;

        public required int CreatorId { get; set; }

        public required int BeatmapSetId { get; set; }

        public required string DifficultyName { get; set; } = null!;

        public double? StarRating { get; set; }

        public required BeatmapStatus Status { get; set; }

        public required int Playcount { get; set; }
    }
}
