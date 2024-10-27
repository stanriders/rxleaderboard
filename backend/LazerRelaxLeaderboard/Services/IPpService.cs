namespace LazerRelaxLeaderboard.Services;

public interface IPpService
{
    Task PopulateScores(bool recalculateAll = false);
    Task PopulateStarRatings();
    Task RecalculatePlayersPp();
    Task RecalculatePlayerPp(int id);
    Task RecalculateStarRatings();
    Task RecalculateBestScores();
    Task RecalculateBestScores(int mapId, int userId);
    Task PopulateScorePp(long id);
    Task CleanupScores();
}
