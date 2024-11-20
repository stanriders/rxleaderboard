namespace LazerRelaxLeaderboard.Config
{
    public class DiscordConfig
    {
        public string Token { get; set; } = null!;

        public ulong GuildId { get; set; }
        public ulong AnnouncementChannelId { get; set; }

        public bool SendAnnouncements { get; set; }
    }
}
