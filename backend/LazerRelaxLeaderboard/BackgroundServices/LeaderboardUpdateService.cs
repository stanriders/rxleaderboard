﻿using LazerRelaxLeaderboard.Database;
using LazerRelaxLeaderboard.OsuApi.Interfaces;
using LazerRelaxLeaderboard.OsuApi.Models;
using Microsoft.EntityFrameworkCore;
using osu.Game.Beatmaps;
using osu.Game.Online.API;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Osu.Mods;
using osu.Game.Rulesets.Osu;
using osu.Game.Rulesets.Scoring;
using osu.Game.Scoring;
using System.Text.Json;

namespace LazerRelaxLeaderboard.BackgroundServices;

public class LeaderboardUpdateService : BackgroundService
{
    private readonly IOsuApiProvider _osuApiProvider;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<LeaderboardUpdateService> _logger;
    private readonly string _cachePath;
    private readonly int _interval;
    private readonly int _batchSize;

    public LeaderboardUpdateService(IOsuApiProvider osuApiProvider, IConfiguration configuration, ILogger<LeaderboardUpdateService> logger, IServiceScopeFactory serviceScopeFactory)
    {
        _osuApiProvider = osuApiProvider;
        _logger = logger;
        _serviceScopeFactory = serviceScopeFactory;
        _interval = int.Parse(configuration["ScoreQueryInterval"]!);
        _batchSize = int.Parse(configuration["ScoreQueryBatch"]!);
        _cachePath = configuration["BeatmapCachePath"]!;
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

                // TODO: automated map list updates
                /*var newMaps = (await File.ReadAllLinesAsync($"{_cachePath}/list.txt", stoppingToken))
                    .Select(x => int.Parse(x[..^4]))
                    .ToArray();*/

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

                    await PopulateBeatmaps(context); // this is not required but might leave just in case?
                    await PopulatePp(context);
                    await PopulatePlayerPp(context);
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
            var modCombos = CreateCombinations(0, Array.Empty<string>(), allowedMods);
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
                        Mods = score.Mods.Select(ModToString).ToArray(),
                        TotalScore = score.TotalScore,
                        UserId = score.User.Id,
                    });
                }

                await Task.Delay(_interval);
            }

            await databaseContext.SaveChangesAsync();
        }
    }
    
    public async Task PopulateBeatmaps(DatabaseContext databaseContext)
    {
        var unpopulatedIds = await databaseContext.Scores.AsNoTracking()
            .Select(x => x.BeatmapId)
            .Distinct()
            .Where(x => !databaseContext.Beatmaps.Any(b => b.Id == x))
            .ToListAsync();

        _logger.LogInformation("Populating beatmaps - {Total} total", unpopulatedIds.Count);

        foreach (var mapId in unpopulatedIds)
        {
            var osuBeatmap = await _osuApiProvider.GetBeatmap((int) mapId);
            if (osuBeatmap != null)
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
                    Status = osuBeatmap.Status
                });
            }

            await Task.Delay(_interval);
        }

        await databaseContext.SaveChangesAsync();

        var unpopulatedStarRatings = await databaseContext.Beatmaps
            .Where(x => x.StarRating == null)
            .ToListAsync();

        _logger.LogInformation("Populating beatmaps sr - {Total} total", unpopulatedStarRatings.Count);

        foreach (var map in unpopulatedStarRatings)
        {
            var mapPath = $"{_cachePath}/{map.Id}.osu";

            var workingBeatmap = new FlatWorkingBeatmap(mapPath);

            var ruleset = new OsuRuleset();
            var difficultyCalculator = ruleset.CreateDifficultyCalculator(workingBeatmap);

            var difficultyAttributes = difficultyCalculator.Calculate(new List<Mod> { new OsuModRelax() });
            map.StarRating = difficultyAttributes.StarRating;
            databaseContext.Beatmaps.Update(map);
        }

        await databaseContext.SaveChangesAsync();
    }
    
    public async Task PopulatePp(DatabaseContext databaseContext)
    {
        var mapScores = await databaseContext.Scores
            .Where(x => x.Pp == null)
            .Where(x => x.Beatmap != null)
            .Include(x => x.Beatmap)
            .Where(x => x.Beatmap!.Status == BeatmapStatus.Ranked || x.Beatmap!.Status == BeatmapStatus.Approved)
            .GroupBy(x => x.BeatmapId)
            .ToListAsync();

        _logger.LogInformation("Populating pp - {Total} total maps", mapScores.Count);
        
        foreach (var mapGroup in mapScores)
        {
            var mapPath = $"{_cachePath}/{mapGroup.Key}.osu";

            var workingBeatmap = new FlatWorkingBeatmap(mapPath);

            var ruleset = new OsuRuleset();
            var difficultyCalculator = ruleset.CreateDifficultyCalculator(workingBeatmap);

            foreach (var modsGroup in mapGroup.GroupBy(x => x.Mods))
            {
                var mods = GetMods(ruleset, modsGroup.Key);
                var difficultyAttributes = difficultyCalculator.Calculate(mods);
                var performanceCalculator = ruleset.CreatePerformanceCalculator();

                foreach (var score in modsGroup)
                {
                    var scoreInfo = new ScoreInfo(workingBeatmap.BeatmapInfo, ruleset.RulesetInfo)
                    {
                        Accuracy = score.Accuracy,
                        MaxCombo = score.Combo,
                        Statistics = new Dictionary<HitResult, int>
                            {
                                { HitResult.Great, score.Count300 },
                                { HitResult.Ok, score.Count100 },
                                { HitResult.Meh, score.Count50 },
                                { HitResult.Miss, score.CountMiss }
                            },
                        Mods = mods,
                        TotalScore = score.TotalScore,
                    };

                    if (score.SliderEnds != null)
                    {
                        scoreInfo.Statistics.Add(HitResult.SliderTailHit, score.SliderEnds.Value);
                    }

                    if (score.SliderTicks != null)
                    {
                        scoreInfo.Statistics.Add(HitResult.LargeTickHit, score.SliderTicks.Value);
                    }

                    if (score.SpinnerBonus != null)
                    {
                        scoreInfo.Statistics.Add(HitResult.LargeBonus, score.SpinnerBonus.Value);
                    }

                    if (score.SpinnerSpins != null)
                    {
                        scoreInfo.Statistics.Add(HitResult.SmallBonus, score.SpinnerSpins.Value);
                    }

                    if (score.LegacySliderEnds != null)
                    {
                        scoreInfo.Statistics.Add(HitResult.SmallTickHit, score.LegacySliderEnds.Value);
                    }

                    var performanceAttributes = performanceCalculator.Calculate(scoreInfo, difficultyAttributes);

                    score.Pp = performanceAttributes.Total;
                    databaseContext.Scores.Update(score);
                }
            }
        }

        await databaseContext.SaveChangesAsync();

        var scoresThatShouldntHavePp = await databaseContext.Scores
            .Where(x => x.Pp != null)
            .Where(x => x.Beatmap != null)
            .Include(x => x.Beatmap)
            .Where(x => x.Beatmap!.Status != BeatmapStatus.Ranked && x.Beatmap!.Status != BeatmapStatus.Approved)
            .ToListAsync();

        _logger.LogInformation("Removing pp from loved maps - {Total} total scores", scoresThatShouldntHavePp.Count);

        foreach (var score in scoresThatShouldntHavePp)
        {
            score.Pp = null;
            databaseContext.Scores.Update(score);
        }

        await databaseContext.SaveChangesAsync();
    }
    
    public async Task PopulatePlayerPp(DatabaseContext databaseContext)
    {
        _logger.LogInformation("Recalculating player pp...");

        var players = await databaseContext.Users.ToListAsync();
        foreach (var player in players)
        {
            // todo: one score per map
            var scores = await databaseContext.Scores.AsNoTracking()
                .Where(x => x.Beatmap != null)
                .Include(x => x.Beatmap)
                .Select(x=> new {x.UserId, BeatmapStatus = x.Beatmap!.Status, x.Pp, x.Accuracy})
                .Where(x => x.UserId == player.Id)
                .Where(x => x.BeatmapStatus == BeatmapStatus.Ranked || x.BeatmapStatus == BeatmapStatus.Approved)
                .Where(x => x.Pp != null)
                .OrderByDescending(x => x.Pp)
                .ToArrayAsync();

            if (!scores.Any())
            {
                player.TotalPp = null;
                player.TotalAccuracy = null;
                databaseContext.Users.Update(player);

                continue;
            }

            // Build the diminishing sum
            double factor = 1;
            double totalPp = 0;
            double totalAccuracy = 0;

            foreach (var score in scores)
            {
                totalPp += score.Pp!.Value * factor;
                totalAccuracy += score.Accuracy * factor;
                factor *= 0.95;
            }

            // We want the percentage, not a factor in [0, 1], hence we divide 20 by 100.
            totalAccuracy *= 100.0 / (20 * (1 - Math.Pow(0.95, scores.Length)));

            // handle floating point precision edge cases.
            totalAccuracy = Math.Clamp(totalAccuracy, 0, 100);

            player.TotalPp = totalPp;
            player.TotalAccuracy = totalAccuracy;
            databaseContext.Users.Update(player);
        }

        await databaseContext.SaveChangesAsync();
    }

    private Mod[] GetMods(Ruleset ruleset, string[] modNames)
    {
        var mods = new List<Mod>();

        foreach (var modName in modNames)
        {
            var mod = ruleset.CreateModFromAcronym(modName);
            if (mod == null)
            {
                var modNameSplit = modName.Split("x");

                mod = ruleset.CreateModFromAcronym(modNameSplit[0]);
                if (mod is ModRateAdjust speedAdjustMod)
                {
                    speedAdjustMod.SpeedChange.Value = double.Parse(modNameSplit[1]);
                    mods.Add(speedAdjustMod);
                }
                else
                {
                    throw new ArgumentException($"Invalid mod provided: {modName}");
                }
            }
            else
            {
                mods.Add(mod);
            }
        }

        return mods.ToArray();
    }

    private static List<string[]> CreateCombinations(int startIndex, string[] pair, string[] initialArray)
    {
        var combinations = new List<string[]>();
        for (int i = startIndex; i < initialArray.Length; i++)
        {
            combinations.Add(pair.Append(initialArray[i]).ToArray());
            combinations.AddRange(CreateCombinations(i + 1, pair.Append(initialArray[i]).ToArray(), initialArray));
        }

        return combinations;
    }

    private string ModToString(APIMod mod)
    {
        if (mod.Settings.ContainsKey("speed_change"))
        {
            var rateChange = mod.Settings.First(x => x.Key == "speed_change");
            var rateChangeValue = (JsonElement) rateChange.Value;

            return $"{mod.Acronym}x{rateChangeValue.GetDouble()}";
        }

        return mod.Acronym;
    }
}
