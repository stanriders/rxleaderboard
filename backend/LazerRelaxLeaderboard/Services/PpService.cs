using System.Collections.Concurrent;
using System.Diagnostics;
using LazerRelaxLeaderboard.Database;
using LazerRelaxLeaderboard.OsuApi.Interfaces;
using LazerRelaxLeaderboard.OsuApi.Models;
using Microsoft.EntityFrameworkCore;
using osu.Game.Beatmaps;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Osu;
using osu.Game.Rulesets.Osu.Mods;
using osu.Game.Rulesets.Scoring;
using osu.Game.Scoring;
using User = LazerRelaxLeaderboard.Database.Models.User;

namespace LazerRelaxLeaderboard.Services;

public class PpService : IPpService
{
    private readonly string _cachePath;
    private readonly DatabaseContext _databaseContext;
    private readonly IDiscordService _discordService;
    private readonly ILogger<IPpService> _logger;
    private readonly IOsuApiProvider _osuApiProvider;

    public PpService(DatabaseContext databaseContext, ILogger<IPpService> logger, IConfiguration configuration,
        IOsuApiProvider osuApiProvider, IDiscordService discordService)
    {
        _databaseContext = databaseContext;
        _logger = logger;
        _osuApiProvider = osuApiProvider;
        _discordService = discordService;
        _cachePath = configuration["BeatmapCachePath"]!;
    }

    public async Task PopulateScores(bool recalculateAll = false)
    {
        var query = _databaseContext.Scores.AsNoTracking();

        if (!recalculateAll)
        {
            query = query.Where(x => x.Pp == null);
        }

        var mapScores = await query
            .Where(x => x.Beatmap != null)
            .Include(x => x.Beatmap)
            .Where(x => x.Beatmap!.Status == BeatmapStatus.Ranked || x.Beatmap!.Status == BeatmapStatus.Approved)
            .GroupBy(x => x.BeatmapId)
            .ToListAsync();

        if (mapScores.Count == 0)
        {
            return;
        }

        _logger.LogInformation("Populating pp - {Total} total maps", mapScores.Count);

        var currentBestPp = await _databaseContext.Scores.AsNoTracking()
            .Where(x => x.Pp != null)
            .Where(x => !x.Hidden)
            .Select(x => x.Pp)
            .OrderByDescending(x => x)
            .FirstOrDefaultAsync();

        for (var i = 0; i < mapScores.Count; i += 100)
        {
            var batch = mapScores.Skip(i).Take(100).ToList();

            var queryBuilder = new ConcurrentBag<string>();
            await Parallel.ForEachAsync(batch, (mapGroup, token) =>
            {
                var mapPath = $"{_cachePath}/{mapGroup.Key}.osu";
                if (!File.Exists(mapPath))
                {
                    _logger.LogError("Couldn't populate score pp - map {Map} file doesn't exist!", mapGroup.Key);

                    return ValueTask.CompletedTask;
                }

                var workingBeatmap = new FlatWorkingBeatmap(mapPath);

                var ruleset = new OsuRuleset();
                var difficultyCalculator = ruleset.CreateDifficultyCalculator(workingBeatmap);

                foreach (var modsGroup in mapGroup.GroupBy(x => x.Mods))
                {
                    var mods = GetMods(ruleset, modsGroup.Key);
                    var difficultyAttributes = difficultyCalculator.Calculate(mods, token);
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
                            TotalScore = score.TotalScore
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

                        if (score.LegacySliderEndMisses != null)
                        {
                            scoreInfo.Statistics.Add(HitResult.SmallTickMiss, score.LegacySliderEndMisses.Value);
                        }

                        if (score.SliderTickMisses != null)
                        {
                            scoreInfo.Statistics.Add(HitResult.LargeTickMiss, score.SliderTickMisses.Value);
                        }

                        var performanceAttributes = performanceCalculator.Calculate(scoreInfo, difficultyAttributes);

                        // ReSharper disable once CompareOfFloatsByEqualityOperator
                        if (score.Pp != performanceAttributes.Total)
                        {
                            if (!recalculateAll && performanceAttributes.Total > currentBestPp * 2)
                            {
                                _logger.LogWarning("Hiding score {Id} - {Pp}pp (current top {CurrentTopPp}pp)", score.Id, performanceAttributes.Total, currentBestPp);
                                _discordService.PostSusScoreAnnouncement(score.Id).Wait(token);
                                queryBuilder.Add($"UPDATE \"Scores\" SET \"Hidden\" = True WHERE \"Id\" = {score.Id};");
                            }

                            queryBuilder.Add($"UPDATE \"Scores\" SET \"Pp\" = {performanceAttributes.Total} WHERE \"Id\" = {score.Id};");
                        }
                    }
                }

                return ValueTask.CompletedTask;
            });

            _logger.LogInformation("Populating scores pp - saving batch of {Total} updates", queryBuilder.Count);

            if (!queryBuilder.IsEmpty)
            {
                await _databaseContext.Database.ExecuteSqlRawAsync(string.Join('\n', queryBuilder));
            }
        }

        // don't post announcements if we're recalculating everything
        if (!recalculateAll)
        {
            var newBestPp = await _databaseContext.Scores.AsNoTracking()
                .Where(x => x.Pp != null)
                .Where(x => !x.Hidden)
                .OrderByDescending(x => x.Pp)
                .FirstOrDefaultAsync();

            if (newBestPp?.Pp > currentBestPp && newBestPp.Pp < currentBestPp * 2)
            {
                _logger.LogInformation(
                    "Posting score {Id} pp record announcement - {Pp}pp (previous top {CurrentTopPp}pp)", newBestPp.Id,
                    newBestPp.Pp, currentBestPp);
                await _discordService.PostBestScoreAnnouncement(newBestPp.Id);
            }
        }
    }

    public async Task PopulateScores(int beatmapId)
    {
        var query = _databaseContext.Scores.AsNoTracking();

        var mapScores = await query
            .Where(x => x.Beatmap != null)
            .Include(x => x.Beatmap)
            .Where(x => x.Beatmap!.Status == BeatmapStatus.Ranked || x.Beatmap!.Status == BeatmapStatus.Approved)
            .Where(x => x.BeatmapId == beatmapId)
            .ToListAsync();

        if (mapScores.Count == 0)
        {
            return;
        }

        _logger.LogInformation("Populating beatmap {Beatmap} pp - {Total} total scores", beatmapId, mapScores.Count);

        var queryBuilder = new ConcurrentBag<string>();

        var mapPath = $"{_cachePath}/{beatmapId}.osu";
        if (!File.Exists(mapPath))
        {
            _logger.LogError("Couldn't populate score pp - map {Map} file doesn't exist!", beatmapId);

            return;
        }

        var workingBeatmap = new FlatWorkingBeatmap(mapPath);

        var ruleset = new OsuRuleset();
        var difficultyCalculator = ruleset.CreateDifficultyCalculator(workingBeatmap);

        foreach (var modsGroup in mapScores.GroupBy(x => x.Mods))
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
                    TotalScore = score.TotalScore
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

                if (score.LegacySliderEndMisses != null)
                {
                    scoreInfo.Statistics.Add(HitResult.SmallTickMiss, score.LegacySliderEndMisses.Value);
                }

                if (score.SliderTickMisses != null)
                {
                    scoreInfo.Statistics.Add(HitResult.LargeTickMiss, score.SliderTickMisses.Value);
                }

                var performanceAttributes = performanceCalculator.Calculate(scoreInfo, difficultyAttributes);

                // ReSharper disable once CompareOfFloatsByEqualityOperator
                if (score.Pp != performanceAttributes.Total)
                {
                    queryBuilder.Add(
                        $"UPDATE \"Scores\" SET \"Pp\" = {performanceAttributes.Total} WHERE \"Id\" = {score.Id};");
                }
            }
        }

        _logger.LogInformation("Populating scores pp - saving batch of {Total} updates", queryBuilder.Count);

        if (!queryBuilder.IsEmpty)
        {
            await _databaseContext.Database.ExecuteSqlRawAsync(string.Join('\n', queryBuilder));
        }
    }

    public async Task PopulateStarRatings()
    {
        var unpopulatedStarRatings = await _databaseContext.Beatmaps
            .Where(x => x.StarRating == null)
            .ToListAsync();

        if (unpopulatedStarRatings.Count == 0)
        {
            return;
        }

        _logger.LogInformation("Populating beatmaps SR - {Total} total", unpopulatedStarRatings.Count);

        foreach (var map in unpopulatedStarRatings)
        {
            var mapPath = $"{_cachePath}/{map.Id}.osu";
            try
            {
                if (!File.Exists(mapPath))
                {
                    _logger.LogWarning("Downloading beatmap {Id}...", map.Id);
                    await _osuApiProvider.DownloadMap(map.Id, mapPath);
                    await Task.Delay(1000); // wait for some time in case of multiple missing maps
                }

                var workingBeatmap = new FlatWorkingBeatmap(mapPath);

                var ruleset = new OsuRuleset();
                var difficultyCalculator = ruleset.CreateDifficultyCalculator(workingBeatmap);

                var difficultyAttributes = difficultyCalculator.Calculate(new List<Mod> { new OsuModRelax() });
                map.StarRating = difficultyAttributes.StarRating;
                _databaseContext.Beatmaps.Update(map);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to populate SR for beatmap {Id}", map.Id);

                // assume that the beatmap is broken
                if (File.Exists(mapPath))
                {
                    File.Delete(mapPath);
                }
            }
        }

        await _databaseContext.SaveChangesAsync();
    }

    public async Task RecalculatePlayersPp()
    {
        _logger.LogInformation("Recalculating all players total pp...");
        var stopwatch = Stopwatch.StartNew();

        var playerCount = await _databaseContext.Users.CountAsync();
        for (var i = 0; i < playerCount; i += 500)
        {
            var players = await _databaseContext.Users
                .Where(x => x.Scores.Count > 0)
                .OrderBy(x => x.Id)
                .Skip(i)
                .Take(500)
                .ToListAsync();

            foreach (var player in players)
            {
                if (await RecalculatePlayerPp(player))
                {
                    _databaseContext.Users.Update(player);
                }
            }

            await _databaseContext.SaveChangesAsync();
        }

        await _databaseContext.SaveChangesAsync();

        _logger.LogInformation("Recalculating all players total pp done! Took {Elapsed}", stopwatch.Elapsed);
    }

    public async Task RecalculatePlayersPp(List<int> playerIds)
    {
        _logger.LogInformation("Recalculating {Count} players total pp...", playerIds.Count);

        var currentTopPlayer = await _databaseContext.Users.AsNoTracking()
            .Where(x => x.TotalPp != null)
            .OrderByDescending(x => x.TotalPp)
            .Select(x => x.Id)
            .FirstOrDefaultAsync();

        for (var i = 0; i < playerIds.Count; i += 500)
        {
            var players = await _databaseContext.Users
                .Where(x => playerIds.Contains(x.Id))
                .Skip(i)
                .Take(500)
                .ToListAsync();

            foreach (var player in players)
            {
                if (await RecalculatePlayerPp(player))
                {
                    _databaseContext.Users.Update(player);
                }
            }

            await _databaseContext.SaveChangesAsync();
        }

        await _databaseContext.SaveChangesAsync();

        var newTopPlayer = await _databaseContext.Users.AsNoTracking()
            .Where(x => x.TotalPp != null)
            .OrderByDescending(x => x.TotalPp)
            .Select(x => x.Id)
            .FirstOrDefaultAsync();

        if (newTopPlayer != currentTopPlayer)
        {
            await _discordService.PostBestPlayerAnnouncement(newTopPlayer);
        }
    }

    public async Task RecalculateStarRatings()
    {
        var maps = await _databaseContext.Beatmaps.AsNoTracking().ToListAsync();

        _logger.LogInformation("Recalculating all maps sr - {Total} maps total", maps.Count);
        var stopwatch = Stopwatch.StartNew();

        const int batchSize = 200;
        for (var i = 0; i < maps.Count; i += batchSize)
        {
            var batch = maps.Skip(i).Take(batchSize).ToList();

            var queryBuilder = new ConcurrentBag<string>();
            await Parallel.ForEachAsync(batch, (map, cancellationToken) =>
            {
                var mapPath = $"{_cachePath}/{map.Id}.osu";
                if (!File.Exists(mapPath))
                {
                    _logger.LogError("Couldn't populate map {Id} sr - map file doesn't exist!", map.Id);

                    return ValueTask.CompletedTask;
                }

                var workingBeatmap = new FlatWorkingBeatmap(mapPath);

                var ruleset = new OsuRuleset();
                var difficultyCalculator = ruleset.CreateDifficultyCalculator(workingBeatmap);

                var difficultyAttributes =
                    difficultyCalculator.Calculate(new List<Mod> { new OsuModRelax() }, cancellationToken);

                // todo: update live SR as well

                // ReSharper disable once CompareOfFloatsByEqualityOperator
                if (map.StarRating != difficultyAttributes.StarRating)
                {
                    queryBuilder.Add(
                        $"UPDATE \"Beatmaps\" SET \"StarRating\" = {difficultyAttributes.StarRating} WHERE \"Id\" = {map.Id};");
                }

                return ValueTask.CompletedTask;
            });

            _logger.LogInformation("Recalculating all maps sr - saving a batch of {Total} updates", queryBuilder.Count);

            if (!queryBuilder.IsEmpty)
            {
                await _databaseContext.Database.ExecuteSqlRawAsync(string.Join('\n', queryBuilder));
            }
        }

        _logger.LogInformation("Recalculating all maps sr done! Took {Elapsed}", stopwatch.Elapsed);
    }

    public async Task RecalculateStarRatings(int beatmapId)
    {
        var map = await _databaseContext.Beatmaps.Where(x => x.Id == beatmapId).FirstOrDefaultAsync();

        if (map == null)
        {
            return;
        }

        _logger.LogInformation("Recalculating map {Beatmap} sr...", beatmapId);

        var mapPath = $"{_cachePath}/{beatmapId}.osu";
        if (!File.Exists(mapPath))
        {
            _logger.LogError("Couldn't populate map {Id} sr - map file doesn't exist!", map.Id);

            return;
        }

        var workingBeatmap = new FlatWorkingBeatmap(mapPath);

        var ruleset = new OsuRuleset();
        var difficultyCalculator = ruleset.CreateDifficultyCalculator(workingBeatmap);
        var difficultyAttributes = difficultyCalculator.Calculate(new List<Mod> { new OsuModRelax() });

        map.StarRating = difficultyAttributes.StarRating;

        try
        {
            var onlineBeatmap = await _osuApiProvider.GetBeatmap(beatmapId);
            if (onlineBeatmap != null)
            {
                map.StarRatingNormal = onlineBeatmap.StarRating;
            }
        }
        catch (Exception ex)
        {
           _logger.LogError(ex, "Couldn't update map {Id} live SR!", map.Id);
        }

        _databaseContext.Beatmaps.Update(map);

        await _databaseContext.SaveChangesAsync();
    }

    public async Task RecalculateBestScores()
    {
        _logger.LogInformation("Recalculating all best scores started...");
        var stopwatch = Stopwatch.StartNew();

        var scoreGroups = await _databaseContext.Scores
            .Where(x => !x.Hidden)
            .GroupBy(x => new { x.BeatmapId, x.UserId })
            .ToArrayAsync();

        foreach (var scoreGroup in scoreGroups)
        {
            var sortedScores = scoreGroup.OrderByDescending(x => x.Pp)
                .ThenByDescending(x => x.TotalScore)
                .ToArray();

            var bestScore = sortedScores.FirstOrDefault();
            if (bestScore != null && !bestScore.IsBest)
            {
                bestScore.IsBest = true;
                _databaseContext.Scores.Update(bestScore);
            }

            foreach (var notBestScore in sortedScores.Skip(1).Where(x => x.IsBest))
            {
                notBestScore.IsBest = false;
                _databaseContext.Scores.Update(notBestScore);
            }
        }

        await _databaseContext.SaveChangesAsync();

        stopwatch.Stop();
        _logger.LogInformation("Recalculating all best scores done! Took {Elapsed}", stopwatch.Elapsed);
    }

    public async Task RecalculateBestScores(List<int> players)
    {
        _logger.LogInformation("Recalculating {Count} players best scores started...", players.Count);

        var scoreGroups = await _databaseContext.Scores
            .Where(x => players.Contains(x.UserId))
            .Where(x => !x.Hidden)
            .GroupBy(x => new { x.BeatmapId, x.UserId })
            .ToArrayAsync();

        foreach (var scoreGroup in scoreGroups)
        {
            var sortedScores = scoreGroup.OrderByDescending(x => x.Pp)
                .ThenByDescending(x => x.TotalScore)
                .ToArray();

            var bestScore = sortedScores.FirstOrDefault();
            if (bestScore != null && !bestScore.IsBest)
            {
                bestScore.IsBest = true;
                _databaseContext.Scores.Update(bestScore);
            }

            foreach (var notBestScore in sortedScores.Skip(1).Where(x => x.IsBest))
            {
                notBestScore.IsBest = false;
                _databaseContext.Scores.Update(notBestScore);
            }
        }

        await _databaseContext.SaveChangesAsync();
    }

    public async Task CleanupScores()
    {
        var scoresThatShouldntHavePp = await _databaseContext.Scores
            .Where(x => x.Pp != null)
            .Where(x => x.Beatmap != null)
            .Include(x => x.Beatmap)
            .Where(x => x.Beatmap!.Status != BeatmapStatus.Ranked && x.Beatmap!.Status != BeatmapStatus.Approved)
            .ToListAsync();

        if (scoresThatShouldntHavePp.Count == 0)
        {
            return;
        }

        _logger.LogInformation("Removing pp from unranked scores - {Total} total scores", scoresThatShouldntHavePp.Count);

        foreach (var score in scoresThatShouldntHavePp)
        {
            score.Pp = null;
            _databaseContext.Scores.Update(score);
        }

        await _databaseContext.SaveChangesAsync();
    }

    public async Task RecalculatePlayerPp(int id)
    {
        var player = await _databaseContext.Users.FindAsync(id);
        if (player == null)
        {
            _logger.LogError("Couldn't recalculate pp for player {Id} - player doesn't exist!", id);

            return;
        }

        var currentTopPlayer = await _databaseContext.Users.AsNoTracking()
            .Where(x => x.TotalPp != null)
            .OrderByDescending(x => x.TotalPp)
            .FirstOrDefaultAsync();

        if (await RecalculatePlayerPp(player))
        {
            _databaseContext.Users.Update(player);

            await _databaseContext.SaveChangesAsync();

            if (player.TotalPp != null &&
                player.TotalPp > currentTopPlayer?.TotalPp &&
                player.Id != currentTopPlayer.Id)
            {
                await _discordService.PostBestPlayerAnnouncement(player.Id);
            }
        }
    }

    private static Mod[] GetMods(Ruleset ruleset, string[] modNames)
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

    private async Task<bool> RecalculatePlayerPp(User player)
    {
        var beforePp = player.TotalPp;
        var beforeAccuracy = player.TotalAccuracy;

        var scores = await _databaseContext.Scores.AsNoTracking()
            .Select(x => new { x.UserId, x.Pp, x.Accuracy, x.BeatmapId, x.Hidden })
            .Where(x => x.Pp != null)
            .Where(x => !x.Hidden)
            .Where(x => x.UserId == player.Id)
            .GroupBy(i => i.BeatmapId)
            .Select(g => g.OrderByDescending(i => i.Pp!).First())
            .ToArrayAsync();

        scores = scores.OrderByDescending(i => i.Pp).Take(1000).ToArray();

        if (scores.Length == 0)
        {
            player.TotalPp = null;
            player.TotalAccuracy = null;

            return beforePp != null || beforeAccuracy != null;
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

        // ReSharper disable CompareOfFloatsByEqualityOperator
        return beforePp != totalPp || beforeAccuracy != totalAccuracy;
        // ReSharper restore CompareOfFloatsByEqualityOperator
    }
}
