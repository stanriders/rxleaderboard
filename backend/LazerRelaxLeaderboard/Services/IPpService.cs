namespace LazerRelaxLeaderboard.Services;

public interface IPpService
{
    Task PopulateScores(bool recalculateAll = false);

    Task PopulateStarRatings();
    Task RecalculateStarRatings();

    Task RecalculatePlayersPp();
    Task RecalculatePlayersPp(List<int> playerIds);
    Task RecalculatePlayerPp(int id);

    Task RecalculateBestScores();
    Task RecalculateBestScores(List<int> players);
    Task RecalculateBestScores(int mapId, int userId);

    Task PopulateScorePp(long id);
    Task CleanupScores();
}
