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

namespace LazerRelaxLeaderboard.Services;

public class PpService : IPpService
{
    private readonly DatabaseContext _databaseContext;
    private readonly IOsuApiProvider _osuApiProvider;
    private readonly ILogger<IPpService> _logger;
    private readonly string _cachePath;

    public PpService(DatabaseContext databaseContext, ILogger<IPpService> logger, IConfiguration configuration, IOsuApiProvider osuApiProvider)
    {
        _databaseContext = databaseContext;
        _logger = logger;
        _osuApiProvider = osuApiProvider;
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

        _logger.LogInformation("Populating pp - {Total} total maps", mapScores.Count);

        for (var i = 0; i < mapScores.Count; i += 500)
        {
            var batch = mapScores.Skip(i).Take(500).ToList();

            var queryBuilder = new ConcurrentBag<string>();
            await Parallel.ForEachAsync(batch, (mapGroup, _) =>
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

                        if (score.LegacySliderEndMisses != null)
                        {
                            scoreInfo.Statistics.Add(HitResult.SmallTickMiss, score.LegacySliderEndMisses.Value);
                        }

                        if (score.SliderTickMisses != null)
                        {
                            scoreInfo.Statistics.Add(HitResult.LargeTickMiss, score.SliderTickMisses.Value);
                        }

                        var performanceAttributes = performanceCalculator.Calculate(scoreInfo, difficultyAttributes);

                        if (score.Pp != performanceAttributes.Total)
                        {
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
    }

    public async Task PopulateStarRatings()
    {
        var unpopulatedStarRatings = await _databaseContext.Beatmaps
            .Where(x => x.StarRating == null)
            .ToListAsync();

        _logger.LogInformation("Populating beatmaps sr - {Total} total", unpopulatedStarRatings.Count);

        foreach (var map in unpopulatedStarRatings)
        {
            var mapPath = $"{_cachePath}/{map.Id}.osu";
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

        await _databaseContext.SaveChangesAsync();
    }

    public async Task RecalculatePlayersPp()
    {
        _logger.LogInformation("Recalculating all players total pp...");
        var stopwatch = Stopwatch.StartNew();

        var playerCount = await _databaseContext.Users.CountAsync();
        for (var i = 0; i < playerCount; i += 100)
        {
            var players = await _databaseContext.Users
                .OrderBy(x => x.Id)
                .Skip(i)
                .Take(100)
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

    public async Task RecalculatePlayerPp(int id)
    {
        var player = await _databaseContext.Users.FindAsync(id);
        if (player == null)
        {
            _logger.LogError("Couldn't recalculate pp for player {Id} - player doesn't exist!", id);
            return;
        }

        if (await RecalculatePlayerPp(player))
        {
            _databaseContext.Users.Update(player);

            await _databaseContext.SaveChangesAsync();
        }
    }

    public async Task RecalculateStarRatings()
    {
        var maps = await _databaseContext.Beatmaps.ToListAsync();

        _logger.LogInformation("Recalculating all maps sr - {Total} maps total", maps.Count);
        var stopwatch = Stopwatch.StartNew();

        await Parallel.ForEachAsync(maps, (map, cancellationToken) =>
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

            var difficultyAttributes = difficultyCalculator.Calculate(new List<Mod> { new OsuModRelax() }, cancellationToken);
            map.StarRating = difficultyAttributes.StarRating;
            // todo: update live SR as well
            _databaseContext.Beatmaps.Update(map);

            return ValueTask.CompletedTask;
        });

        await _databaseContext.SaveChangesAsync();

        _logger.LogInformation("Recalculating all maps sr done! Took {Elapsed}", stopwatch.Elapsed);
    }

    public async Task RecalculateBestScores()
    {
        _logger.LogInformation("Recalculating all best scores started...");
        var stopwatch = Stopwatch.StartNew();

        var scoreGroups = await _databaseContext.Scores.GroupBy(x => new { x.BeatmapId, x.UserId }).ToArrayAsync();
        foreach (var scoreGroup in scoreGroups)
        {
            var sortedScores = scoreGroup.OrderByDescending(x => x.Pp).ToArray();

            var bestScore = sortedScores.FirstOrDefault();
            if (bestScore != null)
            {
                bestScore.IsBest = true;
                _databaseContext.Scores.Update(bestScore);
            }

            foreach (var notBestScore in sortedScores.Skip(1))
            {
                notBestScore.IsBest = false;
                _databaseContext.Scores.Update(notBestScore);
            }
        }

        await _databaseContext.SaveChangesAsync();

        stopwatch.Stop();
        _logger.LogInformation("Recalculating all best scores done! Took {Elapsed}", stopwatch.Elapsed);
    }

    public async Task RecalculateBestScores(int mapId, int userId)
    {
        var scores = await _databaseContext.Scores
            .Where(x => x.UserId == userId && x.BeatmapId == mapId)
            .OrderByDescending(x => x.Pp)
            .ToArrayAsync();

        var bestScore = scores.FirstOrDefault();
        if (bestScore != null)
        {
            bestScore.IsBest = true;
            _databaseContext.Scores.Update(bestScore);
        }

        foreach (var notBestScore in scores.Skip(1))
        {
            notBestScore.IsBest = false;
            _databaseContext.Scores.Update(notBestScore);
        }

        await _databaseContext.SaveChangesAsync();
    }

    public async Task PopulateScorePp(long id)
    {
        var score = await _databaseContext.Scores.FindAsync(id);
        if (score == null)
        {
            _logger.LogError("Couldn't populate score {Id} pp - score doesn't exist!", id);

            return;
        }

        var mapPath = $"{_cachePath}/{score.BeatmapId}.osu";
        if (!File.Exists(mapPath))
        {
            _logger.LogError("Couldn't populate score {Id} pp - map file doesn't exist!", id);

            return;
        }

        var workingBeatmap = new FlatWorkingBeatmap(mapPath);

        var ruleset = new OsuRuleset();
        var difficultyCalculator = ruleset.CreateDifficultyCalculator(workingBeatmap);

        var mods = GetMods(ruleset, score.Mods);
        var difficultyAttributes = difficultyCalculator.Calculate(mods);
        var performanceCalculator = ruleset.CreatePerformanceCalculator();

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

        if (score.LegacySliderEndMisses != null)
        {
            scoreInfo.Statistics.Add(HitResult.SmallTickMiss, score.LegacySliderEndMisses.Value);
        }

        if (score.SliderTickMisses != null)
        {
            scoreInfo.Statistics.Add(HitResult.LargeTickMiss, score.SliderTickMisses.Value);
        }

        var performanceAttributes = performanceCalculator.Calculate(scoreInfo, difficultyAttributes);

        if (score.Pp != performanceAttributes.Total)
        {
            score.Pp = performanceAttributes.Total;
            _databaseContext.Scores.Update(score);
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

        _logger.LogInformation("Removing pp from unranked scores - {Total} total scores", scoresThatShouldntHavePp.Count);

        foreach (var score in scoresThatShouldntHavePp)
        {
            score.Pp = null;
            _databaseContext.Scores.Update(score);
        }

        await _databaseContext.SaveChangesAsync();
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

    private async Task<bool> RecalculatePlayerPp(Database.Models.User player)
    {
        var beforePp = player.TotalPp;
        var beforeAccuracy = player.TotalAccuracy;

        var scores = await _databaseContext.Scores.AsNoTracking()
            .Select(x => new { x.UserId, x.Pp, x.Accuracy, x.BeatmapId })
            .Where(x => x.Pp != null)
            .Where(x => x.UserId == player.Id)
            .GroupBy(i => i.BeatmapId)
            .Select(g => g.OrderByDescending(i => i.Pp!).First())
            .ToArrayAsync();

        scores = scores.OrderByDescending(i => i.Pp).Take(1000).ToArray();

        if (!scores.Any())
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

        return beforePp != totalPp || beforeAccuracy != totalAccuracy;
    }
}
