using LazerRelaxLeaderboard.Database;
using LazerRelaxLeaderboard.OsuApi.Interfaces;
using LazerRelaxLeaderboard.OsuApi.Models;
using Microsoft.EntityFrameworkCore;
using osu.Game.Online.API;
using System.Text.Json;
using LazerRelaxLeaderboard.Services;

namespace LazerRelaxLeaderboard.BackgroundServices;

public class LeaderboardUpdateService : BackgroundService
{
    private readonly IOsuApiProvider _osuApiProvider;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<LeaderboardUpdateService> _logger;
    private readonly int _interval;
    private readonly int _batchSize;

    public LeaderboardUpdateService(IOsuApiProvider osuApiProvider, IConfiguration configuration, ILogger<LeaderboardUpdateService> logger, IServiceScopeFactory serviceScopeFactory)
    {
        _osuApiProvider = osuApiProvider;
        _logger = logger;
        _serviceScopeFactory = serviceScopeFactory;
        _interval = int.Parse(configuration["ScoreQueryInterval"]!);
        _batchSize = int.Parse(configuration["ScoreQueryBatch"]!);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
#if DEBUG
        _logger.LogWarning("LeaderboardUpdateService won't execute on DEBUG");
#else
        while (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("Starting LeaderboardUpdateService loop...");

            try
            {
                using var loopScope = _serviceScopeFactory.CreateScope();
                var context = loopScope.ServiceProvider.GetService<DatabaseContext>();
                if (context == null)
                {
                    _logger.LogError("Couldn't get a database instance!");
                    return;
                }

                var ppService = loopScope.ServiceProvider.GetService<IPpService>();
                if (ppService == null)
                {
                    _logger.LogError("Couldn't get a pp service instance!");
                    return;
                }

                var existingMaps = await context.Scores.AsNoTracking()
                    .Include(x => x.Beatmap)
                    .Where(x => x.Beatmap != null && x.Beatmap.ScoresUpdatedOn < DateTime.UtcNow.AddDays(-7))
                    .GroupBy(x=> x.BeatmapId)
                    .OrderByDescending(x=> x.Count())
                    .Select(x => x.Key)
                    .ToArrayAsync(cancellationToken: stoppingToken);

                var scorelessMaps = await context.Beatmaps.AsNoTracking()
                    .Where(x => x.ScoresUpdatedOn < DateTime.UtcNow.AddDays(-7))
                    .Select(x => x.Id)
                    .Where(x => !existingMaps.Contains(x))
                    .ToArrayAsync(cancellationToken: stoppingToken);

                /*var newMaps = (await File.ReadAllLinesAsync($"{_cachePath}/list.txt", stoppingToken))
                    .Select(x => int.Parse(x[..^4]))
                    .ToArray();*/

                var totalMaps = existingMaps.Concat(scorelessMaps.OrderBy(_ => Random.Shared.Next())).ToArray();

                for (var i = 0; i < totalMaps.Length; i += _batchSize)
                {
                    _logger.LogInformation("Starting new batch of {BatchSize} ({Current}/{Total})", _batchSize, i, totalMaps.Length);

                    // we're catching score collection exceptions separately to run beatmap/pp population regardless of its fails
                    try
                    {
                        await CollectScores(totalMaps.Skip(i).Take(_batchSize).ToArray(), context);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "LeaderboardUpdateService.CollectScores failed!");
                    }

                    await ppService.PopulateStarRatings();
                    await ppService.PopulateScores();
                    await ppService.CleanupScores();
                    await ppService.RecalculatePlayersPp();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "LeaderboardUpdateService failed!");
            }

            _logger.LogInformation("Finished LeaderboardUpdateService loop");

            await Task.Delay(5000, stoppingToken);
        }
#endif
    }

    public async Task CollectScores(int[] maps, DatabaseContext databaseContext)
    {
        foreach (var mapId in maps)
        {
            _logger.LogInformation("Processing {MapId}...", mapId);

            var dbBeatmap = await databaseContext.Beatmaps.FindAsync(mapId);
            if (dbBeatmap != null)
            {
                dbBeatmap.ScoresUpdatedOn = DateTime.UtcNow;
                // don't save here, only save the date together with scores
            }
            //else BRING BACK AFTER ALL MINIGAME MAPS ARE GONE
            {
                var osuBeatmap = await _osuApiProvider.GetBeatmap(mapId);
                if (osuBeatmap == null)
                {
                    _logger.LogWarning("Beatmap {Id} was not found on osu!API", mapId);

                    // wait until querying api again
                    await Task.Delay(_interval);
                    continue;
                }

                if (osuBeatmap.Mode != Mode.Osu)
                {
                    _logger.LogWarning("Beatmap {Id} mode is not osu! ({Mode})", mapId, osuBeatmap.Mode);
                    if (dbBeatmap != null)
                    {
                        _logger.LogWarning("Removing beatmap {Id}...", mapId);

                        var scores = await databaseContext.Scores.Where(x=> x.BeatmapId == dbBeatmap.Id).ToListAsync();
                        if (scores.Any())
                            databaseContext.Scores.RemoveRange(scores);

                        databaseContext.Beatmaps.Remove(dbBeatmap);
                        await databaseContext.SaveChangesAsync();
                    }

                    await Task.Delay(_interval);
                    continue;
                }

                // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
                // beatmapsets should never be null, however SOMEHOW they sometimes are
                if (osuBeatmap.BeatmapSet == null)
                {
                    _logger.LogWarning("Beatmap {Id} is broken", mapId);

                    await Task.Delay(_interval);
                    continue;
                }

                if (dbBeatmap == null)
                {
                    await databaseContext.Beatmaps.AddAsync(new Database.Models.Beatmap
                    {
                        Id = osuBeatmap.Id,
                        ApproachRate = osuBeatmap.ApproachRate,
                        Artist = osuBeatmap.BeatmapSet.Artist,
                        BeatmapSetId = osuBeatmap.BeatmapSet.Id,
                        BeatsPerMinute = osuBeatmap.BeatsPerMinute,
                        CircleSize = osuBeatmap.CircleSize,
                        Circles = osuBeatmap.Circles,
                        CreatorId = osuBeatmap.BeatmapSet.CreatorId,
                        DifficultyName = osuBeatmap.Version,
                        HealthDrain = osuBeatmap.HealthDrain,
                        Title = osuBeatmap.BeatmapSet.Title,
                        OverallDifficulty = osuBeatmap.OverallDifficulty,
                        Sliders = osuBeatmap.Sliders,
                        Spinners = osuBeatmap.Spinners,
                        StarRatingNormal = osuBeatmap.StarRating,
                        MaxCombo = osuBeatmap.MaxCombo,
                        Status = osuBeatmap.Status,
                        ScoresUpdatedOn = DateTime.UtcNow
                    });
                }

                await databaseContext.SaveChangesAsync();

                // wait until querying api again
                await Task.Delay(_interval);
            }

            var existingScores = await databaseContext.Scores.AsNoTracking()
                .Where(x => x.BeatmapId == mapId)
                .Select(x => x.Id)
                .ToArrayAsync();

            var allowedMods = new[] { "HD", "DT", "HR" };
            var modCombos = Utils.CreateCombinations(0, Array.Empty<string>(), allowedMods);
            modCombos.Add(new[] { string.Empty });

            foreach (var modCombo in modCombos)
            {
                var scores = await _osuApiProvider.GetScores(mapId, modCombo);
                if (scores == null || scores.Scores.Length == 0)
                {
                    await Task.Delay(_interval);

                    continue;
                }

                // only allow new scores w/o settings AND rate changes
                var filteredScores = scores.Scores
                    .Where(s => s.Mods.All(m => m.Settings.Count == 0 || m.Settings.Keys.All(x=> x == "speed_change")))
                    .Where(x => !existingScores.Contains(x.Id));

                foreach (var score in filteredScores)
                {
                    var user = await databaseContext.Users.FindAsync(score.User.Id);
                    if (user != null)
                    {
                        user.CountryCode = score.User.CountryCode;
                        user.UpdatedAt = DateTime.UtcNow;
                        user.Username = score.User.Username;
                    }
                    else
                    {
                        await databaseContext.Users.AddAsync(new Database.Models.User
                        {
                            Id = score.User.Id,
                            CountryCode = score.User.CountryCode,
                            UpdatedAt = DateTime.UtcNow,
                            Username = score.User.Username
                        });
                    }

                    await databaseContext.Scores.AddAsync(new Database.Models.Score
                    {
                        Id = score.Id,
                        Accuracy = score.Accuracy,
                        BeatmapId = score.BeatmapId,
                        Combo = score.Combo,
                        Count100 = score.Statistics.Count100,
                        Count300 = score.Statistics.Count300,
                        Count50 = score.Statistics.Count50,
                        CountMiss = score.Statistics.CountMiss,
                        SliderEnds = score.Statistics.SliderEnds,
                        SliderTicks = score.Statistics.SliderTicks,
                        SpinnerBonus = score.Statistics.SpinnerBonus,
                        SpinnerSpins = score.Statistics.SpinnerSpins,
                        LegacySliderEnds = score.Statistics.LegacySliderEnds,
                        Date = score.Date,
                        Grade = score.Grade,
                        Mods = score.Mods.Select(Utils.ModToString).ToArray(),
                        TotalScore = score.TotalScore,
                        UserId = score.User.Id,
                    });
                }

                await Task.Delay(_interval);
            }

            await databaseContext.SaveChangesAsync();
        }
    }
}
