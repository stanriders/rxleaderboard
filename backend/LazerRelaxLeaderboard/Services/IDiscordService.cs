namespace LazerRelaxLeaderboard.Services;

public interface IDiscordService
{
    Task PostBestScoreAnnouncement(long scoreId);
    Task PostBestPlayerAnnouncement(int userId);
    Task PostSusScoreAnnouncement(long scoreId);
}
