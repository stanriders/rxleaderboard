namespace LazerRelaxLeaderboard.Services;

public interface IPpService
{
    Task PopulateScores(bool recalculateAll = false);
    Task PopulateScores(int beatmapId);

    Task PopulateStarRatings();
    Task RecalculateStarRatings();
    Task RecalculateStarRatings(int beatmapId);

    Task RecalculatePlayersPp();
    Task RecalculatePlayersPp(List<int> playerIds);

    Task RecalculateBestScores();
    Task RecalculateBestScores(List<int> players);

    Task CleanupScores();
}
