using Discord;
using Discord.WebSocket;
using LazerRelaxLeaderboard.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using DiscordConfig = LazerRelaxLeaderboard.Config.DiscordConfig;

namespace LazerRelaxLeaderboard.Services;

public class DiscordBackgroundService : BackgroundService
{
    private readonly DiscordSocketClient _client;

    private readonly DiscordConfig _discordConfig;
    private readonly ILogger<DiscordBackgroundService> _logger;

    public DiscordBackgroundService(IOptions<DiscordConfig> configuration, ILogger<DiscordBackgroundService> logger,
        DiscordSocketClient client)
    {
        _logger = logger;
        _client = client;

        _discordConfig = configuration.Value;

        _client.Log += Log;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var _ = _logger.BeginScope("Discord");

        _logger.LogInformation("Starting discord service...");

        await _client.LoginAsync(TokenType.Bot, _discordConfig.Token);
        await _client.StartAsync();

        while (!stoppingToken.IsCancellationRequested)
        {
            // do stuff?
            await Task.Delay(1000, stoppingToken);
        }

        await _client.StopAsync();
    }

    private Task Log(LogMessage message)
    {
        var severity = message.Severity switch
        {
            LogSeverity.Critical => LogLevel.Critical,
            LogSeverity.Error => LogLevel.Error,
            LogSeverity.Warning => LogLevel.Warning,
            LogSeverity.Info => LogLevel.Information,
            LogSeverity.Verbose => LogLevel.Debug,
            LogSeverity.Debug => LogLevel.Trace,
            _ => throw new ArgumentException()
        };

        _logger.Log(severity, message.Exception, "[{Source}] {Message}", message.Source, message.Message);

        return Task.CompletedTask;
    }
}

public class DiscordService : IDiscordService
{
    private readonly DatabaseContext _databaseContext;
    private readonly DiscordConfig _discordConfig;
    private readonly DiscordSocketClient _discordSocketClient;
    private readonly ILogger<IDiscordService> _logger;

    public DiscordService(ILogger<IDiscordService> logger, DiscordSocketClient discordSocketClient,
        IOptions<DiscordConfig> configuration, DatabaseContext databaseContext)
    {
        _logger = logger;
        _discordSocketClient = discordSocketClient;
        _discordConfig = configuration.Value;
        _databaseContext = databaseContext;
    }

    public async Task PostBestScoreAnnouncement(long scoreId)
    {
        if (!_discordConfig.SendAnnouncements)
        {
            return;
        }

        var guild = _discordSocketClient.GetGuild(_discordConfig.GuildId);
        if (guild == null)
        {
            _logger.LogError("Guild {GuildId} not found!", _discordConfig.GuildId);
            return;
        }

        var verifiedChannel = guild.GetTextChannel(_discordConfig.AnnouncementChannelId);
        if (verifiedChannel == null)
        {
            _logger.LogError("Announcement channel {Channel} not found!", verifiedChannel);
            return;
        }

        var score = _databaseContext.Scores.AsNoTracking()
            .Include(x => x.Beatmap)
            .Include(x => x.User)
            .Where(x => !x.Hidden)
            .FirstOrDefault(x => x.Id == scoreId);

        if (score == null)
        {
            _logger.LogError("Non-existing score {Score}", scoreId);
            return;
        }

        var description =
            $"[{score.Beatmap!.Artist} - {score.Beatmap.Title} [{score.Beatmap.DifficultyName}]](https://osu.ppy.sh/b/{score.BeatmapId}) +**{string.Join("", score.Mods)}**\n" +
            $"**{score.Grade}** · {score.TotalScore} · {score.Combo}x ({score.Count300} / {score.Count100} / {score.Count50} / {score.CountMiss})\n" +
            $"**{score.Pp:N2}pp**";

        await verifiedChannel.SendMessageAsync(
            embed: new EmbedBuilder()
                .WithTitle($"New PP record by {score.User.Username}!")
                .WithUrl($"https://osu.ppy.sh/scores/{score.Id}") // todo: in-site score link
                .WithDescription(description)
                .WithThumbnailUrl($"https://a.ppy.sh/{score.User.Id}")
                .WithColor(Color.Gold)
                .WithTimestamp(score.Date)
                .WithFooter("Relaxation Vault", "https://rx.stanr.info/rv-yellowlight-192.png")
                .Build()
        );
    }

    public async Task PostBestPlayerAnnouncement(int userId)
    {
        if (!_discordConfig.SendAnnouncements)
        {
            return;
        }

        var guild = _discordSocketClient.GetGuild(_discordConfig.GuildId);
        if (guild == null)
        {
            _logger.LogError("Guild {GuildId} not found!", _discordConfig.GuildId);
            return;
        }

        var verifiedChannel = guild.GetTextChannel(_discordConfig.AnnouncementChannelId);
        if (verifiedChannel == null)
        {
            _logger.LogError("Announcement channel {Channel} not found!", verifiedChannel);
            return;
        }

        var user = _databaseContext.Users.AsNoTracking().FirstOrDefault(x => x.Id == userId);

        if (user == null)
        {
            _logger.LogError("Non-existing user {User}", userId);
            return;
        }

        await verifiedChannel.SendMessageAsync(
            embed: new EmbedBuilder()
                .WithAuthor($"{user.Username} - {user.TotalPp:N2}pp", $"https://a.ppy.sh/{user.Id}",
                    $"https://rx.stanr.info/users/{user.Id}")
                .WithDescription($"**{user.Username}** has taken **#1** place on the leaderboard!")
                .WithColor(Color.Gold)
                .WithTimestamp(DateTimeOffset.UtcNow)
                .WithFooter("Relaxation Vault", "https://rx.stanr.info/rv-yellowlight-192.png")
                .Build()
        );
    }

    public async Task PostSusScoreAnnouncement(long scoreId)
    {
        if (!_discordConfig.SendAnnouncements)
        {
            return;
        }

        var guild = _discordSocketClient.GetGuild(_discordConfig.GuildId);
        if (guild == null)
        {
            _logger.LogError("Guild {GuildId} not found!", _discordConfig.GuildId);
            return;
        }

        var reportChannel = guild.GetTextChannel(_discordConfig.ReportChannelId);
        if (reportChannel == null)
        {
            _logger.LogError("Announcement channel {Channel} not found!", reportChannel);
            return;
        }

        var score = _databaseContext.Scores.AsNoTracking()
            .Include(x => x.Beatmap)
            .Include(x => x.User)
            .FirstOrDefault(x => x.Id == scoreId);

        if (score == null)
        {
            _logger.LogError("Non-existing score {Score}", scoreId);
            return;
        }

        var description =
            $"[{score.Beatmap!.Artist} - {score.Beatmap.Title} [{score.Beatmap.DifficultyName}]](https://osu.ppy.sh/b/{score.BeatmapId}) +**{string.Join("", score.Mods)}**\n" +
            $"**{score.Grade}** · {score.TotalScore} · {score.Combo}x ({score.Count300} / {score.Count100} / {score.Count50} / {score.CountMiss})\n" +
            $"**{score.Pp:N2}pp**";

        await reportChannel.SendMessageAsync(
            embed: new EmbedBuilder()
                .WithTitle($"New PP record by {score.User.Username}!")
                .WithUrl($"https://osu.ppy.sh/scores/{score.Id}") // todo: in-site score link
                .WithDescription(description)
                .WithThumbnailUrl($"https://a.ppy.sh/{score.User.Id}")
                .WithColor(Color.Red)
                .WithTimestamp(score.Date)
                .WithFooter("Relaxation Vault", "https://rx.stanr.info/rv-yellowlight-192.png")
                .Build()
        );
    }
}
