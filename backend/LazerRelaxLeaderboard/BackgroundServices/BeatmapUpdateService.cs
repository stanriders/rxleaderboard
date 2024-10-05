using LazerRelaxLeaderboard.Database;
using LazerRelaxLeaderboard.OsuApi.Interfaces;
using LazerRelaxLeaderboard.OsuApi.Models;
using Microsoft.EntityFrameworkCore;

namespace LazerRelaxLeaderboard.BackgroundServices;

// TEMP: remove when all maps get their status populated
// TODO: same for scores 
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
        _logger.LogInformation("Starting BeatmapUpdateService loop...");

        try
        {
            using var loopScope = _serviceScopeFactory.CreateScope();
            var context = loopScope.ServiceProvider.GetService<DatabaseContext>();
            if (context == null)
            {
                _logger.LogError("Couldn't get a database instance!");

                return;
            }

            var maps = await context.Scores.AsNoTracking()
                .Where(x => x.Pp != null && x.Beatmap != null)
                .Include(x => x.Beatmap)
                .Where(x => x.Beatmap!.Status == null)
                .Select(x=> new {x.Id, x.BeatmapId, x.Pp})
                .OrderByDescending(x => x.Pp)
                .GroupBy(x => x.BeatmapId)
                .ToArrayAsync(cancellationToken: stoppingToken);

            for (var i = 0; i < maps.Length; i++)
            {
                _logger.LogInformation("Updating map {Id} ({Current}/{Total})", maps[i].Key, i, maps.Length);

                var osuBeatmap = await _osuApiProvider.GetBeatmap(maps[i].Key);
                if (osuBeatmap != null)
                {
                    var dbMap = await context.Beatmaps.FindAsync(osuBeatmap.Id);
                    if (dbMap != null)
                    {
                        dbMap.Status = osuBeatmap.Status;
                        dbMap.MaxCombo = osuBeatmap.MaxCombo;

                        if (osuBeatmap.Status != BeatmapStatus.Ranked && osuBeatmap.Status != BeatmapStatus.Approved)
                        {
                            _logger.LogInformation("Nullifying pp for scores on map {Id}...", maps[i].Key);

                            var scores = await context.Scores
                                .Where(x => x.Pp != null && x.BeatmapId == osuBeatmap.Id)
                                .ToListAsync(cancellationToken: stoppingToken);

                            foreach (var score in scores)
                            {
                                score.Pp = null;
                            }
                        }
                    }
                }

                await context.SaveChangesAsync(stoppingToken);

                await Task.Delay((int)(_interval * 1.5), stoppingToken);
            }

            await context.SaveChangesAsync(stoppingToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "BeatmapUpdateService failed!");
        }

        _logger.LogInformation("Finished BeatmapUpdateService loop");
    }
}
