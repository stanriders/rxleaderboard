using LazerRelaxLeaderboard.OsuApi.Models;

namespace LazerRelaxLeaderboard.OsuApi.Interfaces;

public interface IOsuApiProvider
{
    Task<BeatmapScores?> GetScores(int id, string[] mods);
    Task<Beatmap?> GetBeatmap(int id);
    Task<Score?> GetScore(long id);
    Task<bool> DownloadMap(int id, string path);
}
