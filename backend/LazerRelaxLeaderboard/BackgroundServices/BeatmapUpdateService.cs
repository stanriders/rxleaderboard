using LazerRelaxLeaderboard.Database;
using LazerRelaxLeaderboard.OsuApi.Interfaces;
using LazerRelaxLeaderboard.OsuApi.Models;
using Microsoft.EntityFrameworkCore;
using osu.Game.Rulesets.Osu.Beatmaps;

namespace LazerRelaxLeaderboard.BackgroundServices;

// TEMP: remove when all scores get their lazer statistics populated
public class BeatmapUpdateService : BackgroundService
{
    private readonly IOsuApiProvider _osuApiProvider;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<LeaderboardUpdateService> _logger;
    private readonly int _interval;

    public BeatmapUpdateService(IOsuApiProvider osuApiProvider, IServiceScopeFactory serviceScopeFactory, ILogger<LeaderboardUpdateService> logger, IConfiguration configuration)
    {
        _osuApiProvider = osuApiProvider;
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
        _interval = int.Parse(configuration["ScoreQueryInterval"]!);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Starting ScoreUpdateService loop...");

        try
        {
            using var loopScope = _serviceScopeFactory.CreateScope();
            var context = loopScope.ServiceProvider.GetService<DatabaseContext>();
            if (context == null)
            {
                _logger.LogError("Couldn't get a database instance!");

                return;
            }

            var scores = await context.Scores.AsNoTracking()
                .Where(x => x.Pp != null)
                .Select(x => new {x.Id})
                .ToArrayAsync(cancellationToken: stoppingToken);

            for (var i = 0; i < scores.Length; i++)
            {
                _logger.LogInformation("Updating score {Id} ({Current}/{Total})", scores[i].Id, i, scores.Length);

                var dbScore = await context.Scores.FindAsync(scores[i].Id);
                if (dbScore != null)
                {
                    var osuScore = await _osuApiProvider.GetScore(scores[i].Id);
                    if (osuScore == null)
                    {
                        _logger.LogWarning("Score {Id} doesn't exist according to osu!API!!!", dbScore.Id);
                        context.Scores.Remove(dbScore);
                        await context.SaveChangesAsync(stoppingToken);

                        continue;
                    }

                    dbScore.LegacySliderEnds = osuScore.Statistics.LegacySliderEnds;
                    dbScore.SliderEnds = osuScore.Statistics.SliderEnds;
                    dbScore.SliderTicks = osuScore.Statistics.SliderTicks;
                    dbScore.SpinnerBonus = osuScore.Statistics.SpinnerBonus;
                    dbScore.SpinnerSpins = osuScore.Statistics.SpinnerSpins;
                }

                await context.SaveChangesAsync(stoppingToken);

                await Task.Delay((int)(_interval * 1.5), stoppingToken);
            }

            await context.SaveChangesAsync(stoppingToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ScoreUpdateService failed!");
        }

        _logger.LogInformation("Finished ScoreUpdateService loop");
    }
}
