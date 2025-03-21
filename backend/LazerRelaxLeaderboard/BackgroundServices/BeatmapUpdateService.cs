﻿using LazerRelaxLeaderboard.Database;
using LazerRelaxLeaderboard.OsuApi.Interfaces;
using LazerRelaxLeaderboard.OsuApi.Models;
using LazerRelaxLeaderboard.Services;
using Microsoft.EntityFrameworkCore;
using Beatmap = LazerRelaxLeaderboard.Database.Models.Beatmap;
using Score = LazerRelaxLeaderboard.Database.Models.Score;
using User = LazerRelaxLeaderboard.Database.Models.User;

namespace LazerRelaxLeaderboard.BackgroundServices;

// TODO: this can be removed after the automatic score pumping is confirmed to be working properly
public class BeatmapUpdateService : BackgroundService
{
    private readonly int _batchSize;
    private readonly bool _enableProcessing;
    private readonly int _interval;
    private readonly ILogger<BeatmapUpdateService> _logger;
    private readonly IOsuApiProvider _osuApiProvider;
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public BeatmapUpdateService(IOsuApiProvider osuApiProvider, IConfiguration configuration,
        ILogger<BeatmapUpdateService> logger, IServiceScopeFactory serviceScopeFactory)
    {
        _osuApiProvider = osuApiProvider;
        _logger = logger;
        _serviceScopeFactory = serviceScopeFactory;
        _interval = int.Parse(configuration["APIQueryInterval"]!);
        _batchSize = int.Parse(configuration["BeatmapQueryBatch"]!);
        _enableProcessing = bool.Parse(configuration["EnableBeatmapProcessing"]!);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_enableProcessing)
        {
            _logger.LogWarning("BeatmapUpdateService is disabled");

            return;
        }

        while (!stoppingToken.IsCancellationRequested)
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

                var scoredMaps = await context.Scores.AsNoTracking()
                    .Include(x => x.Beatmap)
                    .Where(x => x.Beatmap != null)
                    .GroupBy(x => x.BeatmapId)
                    .OrderByDescending(x => x.Count())
                    .Select(x => x.Key)
                    .ToArrayAsync(stoppingToken);

                var scorelessMaps = await context.Beatmaps.AsNoTracking()
                    .Select(x => x.Id)
                    .Where(x => !scoredMaps.Contains(x))
                    .ToArrayAsync(stoppingToken);

                var existingMaps = scoredMaps.Concat(scorelessMaps.OrderBy(_ => Random.Shared.Next())).ToArray();

                for (var i = 0; i < existingMaps.Length; i += _batchSize)
                {
                    _logger.LogInformation("Starting new batch of {BatchSize} ({Current}/{Total})", _batchSize, i,
                        existingMaps.Length);

                    using var batchScope = _serviceScopeFactory.CreateScope();
                    var ppService = batchScope.ServiceProvider.GetService<IPpService>();
                    if (ppService == null)
                    {
                        _logger.LogError("Couldn't get a pp service instance!");
                        return;
                    }

                    (int collectedScores, List<int> affectedPlayers)? collectionResult = null;

                    // we're catching score collection exceptions separately to run beatmap/pp population regardless of its fails
                    try
                    {
                        collectionResult =
                            await CollectScores(existingMaps.Skip(i).Take(_batchSize).ToArray(), context);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "BeatmapUpdateService.CollectScores failed!");
                    }

                    await ppService.PopulateStarRatings();

                    // no real need to waste time on processing all these if we collected zero new scores
                    if (collectionResult?.collectedScores > 0)
                    {
                        await ppService.PopulateScores();
                        await ppService.CleanupScores();
                        await ppService.RecalculateBestScores(collectionResult.Value.affectedPlayers);
                        await ppService.RecalculatePlayersPp(collectionResult.Value.affectedPlayers);
                    }

                    // recalculate all players pp every 10 batches just in case
                    if (i % (_batchSize * 50) == 0)
                    {
                        await ppService.RecalculateBestScores();
                        await ppService.RecalculatePlayersPp();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "BeatmapUpdateService failed!");
                await Task.Delay(_interval, stoppingToken);
            }

            _logger.LogInformation("Finished BeatmapUpdateService loop");

            await Task.Delay(5000, stoppingToken);
        }
    }

    public async Task<(int collectedScores, List<int> affectedPlayers)> CollectScores(int[] maps,
        DatabaseContext databaseContext)
    {
        var collectedScores = 0;
        var affectedPlayers = new List<int>();

        foreach (var mapId in maps)
        {
            _logger.LogInformation("Processing {MapId}...", mapId);

            var dbBeatmap = await databaseContext.Beatmaps.FindAsync(mapId);
            if (dbBeatmap == null)
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
                    await databaseContext.Beatmaps.AddAsync(new Beatmap
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
                        Status = osuBeatmap.Status
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
                var scores = await _osuApiProvider.GetBeatmapScores(mapId, modCombo);
                if (scores == null || scores.Scores.Length == 0)
                {
                    await Task.Delay(_interval);

                    continue;
                }

                // only allow new scores w/o settings AND rate changes
                var filteredScores = scores.Scores
                    .Where(s => s.Mods.All(m => m.Settings.Count == 0 || m.Settings.Keys.All(x => x == "speed_change")))
                    .Where(x => !existingScores.Contains(x.Id));

                foreach (var score in filteredScores)
                {
                    var user = await databaseContext.Users.FindAsync(score.User!.Id);
                    if (user != null)
                    {
                        user.CountryCode = score.User.CountryCode;
                        user.UpdatedAt = DateTime.UtcNow;
                        user.Username = score.User.Username;
                    }
                    else
                    {
                        await databaseContext.Users.AddAsync(new User
                        {
                            Id = score.User.Id,
                            CountryCode = score.User.CountryCode,
                            UpdatedAt = DateTime.UtcNow,
                            Username = score.User.Username
                        });
                    }

                    await databaseContext.Scores.AddAsync(new Score
                    {
                        Id = score.Id,
                        Accuracy = score.Accuracy,
                        BeatmapId = score.BeatmapId,
                        Combo = score.Combo,
                        Count100 = score.Statistics.Count100 ?? 0,
                        Count300 = score.Statistics.Count300,
                        Count50 = score.Statistics.Count50 ?? 0,
                        CountMiss = score.Statistics.CountMiss ?? 0,
                        SliderEnds = score.Statistics.SliderEnds,
                        SliderTicks = score.Statistics.SliderTicks,
                        SpinnerBonus = score.Statistics.SpinnerBonus,
                        SpinnerSpins = score.Statistics.SpinnerSpins,
                        LegacySliderEnds = score.Statistics.LegacySliderEnds,
                        LegacySliderEndMisses = score.Statistics.LegacySliderEndMisses,
                        SliderTickMisses = score.Statistics.SliderTickMisses,
                        Date = score.Date,
                        Grade = score.Grade,
                        Mods = score.Mods.Select(Utils.ModToString).ToArray(),
                        TotalScore = score.TotalScore,
                        UserId = score.User.Id,
                        IsBest = false
                    });

                    if (!affectedPlayers.Contains(score.User.Id))
                    {
                        affectedPlayers.Add(score.User.Id);
                    }

                    collectedScores++;
                }

                await Task.Delay(_interval);
            }

            await databaseContext.SaveChangesAsync();
        }

        return (collectedScores, affectedPlayers);
    }
}
