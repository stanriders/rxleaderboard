using System.Collections.Concurrent;
using LazerRelaxLeaderboard.Database;
using LazerRelaxLeaderboard.OsuApi.Interfaces;
using LazerRelaxLeaderboard.OsuApi.Models;
using LazerRelaxLeaderboard.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using osu.Game.Beatmaps;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Osu;
using osu.Game.Rulesets.Osu.Mods;
using osu.Game.Rulesets.Scoring;
using osu.Game.Scoring;

namespace LazerRelaxLeaderboard.Controllers;

[ApiController]
[Route("[controller]")]
public class AdminController : ControllerBase
{
    private readonly IOsuApiProvider _osuApiProvider;
    private readonly DatabaseContext _databaseContext;
    private readonly IKeyAuthService _authService;
    private readonly ILogger<AdminController> _logger;
    private readonly string _cachePath;
    private readonly int _interval;

    public AdminController(IOsuApiProvider osuApiProvider, DatabaseContext databaseContext, IConfiguration configuration, ILogger<AdminController> logger, IKeyAuthService authService)
    {
        _osuApiProvider = osuApiProvider;
        _databaseContext = databaseContext;
        _logger = logger;
        _authService = authService;
        _interval = int.Parse(configuration["ScoreQueryInterval"]!);
        _cachePath = configuration["BeatmapCachePath"]!;
    }
        
    [HttpPost("populateBeatmaps")]
    public async Task<IActionResult> PopulateBeatmaps()
    {
        if (!_authService.Authorize(HttpContext))
        {
            return Unauthorized();
        }

        var unpopulatedIds = await _databaseContext.Scores.AsNoTracking()
            .Select(x => x.BeatmapId)
            .Distinct()
            .Where(x => !_databaseContext.Beatmaps.Any(b => b.Id == x))
            .ToListAsync();

        foreach (var mapId in unpopulatedIds)
        {
            var osuBeatmap = await _osuApiProvider.GetBeatmap((int)mapId);
            if (osuBeatmap != null)
            {
                await _databaseContext.Beatmaps.AddAsync(new Database.Models.Beatmap
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

        await _databaseContext.SaveChangesAsync();

        var unpopulatedStarRatings = await _databaseContext.Beatmaps
            .Where(x=> x.StarRating == null)
            .ToListAsync();

        foreach (var map in unpopulatedStarRatings)
        {
            var mapPath = $"{_cachePath}/{map.Id}.osu";

            var workingBeatmap = new FlatWorkingBeatmap(mapPath);

            var ruleset = new OsuRuleset();
            var difficultyCalculator = ruleset.CreateDifficultyCalculator(workingBeatmap);

            var difficultyAttributes = difficultyCalculator.Calculate(new List<Mod> { new OsuModRelax() });
            map.StarRating = difficultyAttributes.StarRating;
            _databaseContext.Beatmaps.Update(map);
        }

        await _databaseContext.SaveChangesAsync();

        return Ok(unpopulatedIds.Count);
    }

    [HttpPost("recalculateStarRatings")]
    public async Task<IActionResult> PopulateSr()
    {
        if (!_authService.Authorize(HttpContext))
        {
            return Unauthorized();
        }

        var maps = await _databaseContext.Beatmaps.ToListAsync();

        _logger.LogInformation("Populating all maps sr - {Total} maps total", maps.Count);

        await Parallel.ForEachAsync(maps, (map, cancellationToken) =>
        {
            var mapPath = $"{_cachePath}/{map.Id}.osu";

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

        _logger.LogInformation("Populating all maps sr done!");

        return Ok(maps.Count);
    }


    [HttpPost("recalculatePp")]
    public async Task<IActionResult> PopulatePp()
    {
        if (!_authService.Authorize(HttpContext))
        {
            return Unauthorized();
        }

        var mapScores = await _databaseContext.Scores.AsNoTracking()
            .Where(x=> x.Beatmap != null)
            .Include(x=> x.Beatmap)
            .Where(x => x.Beatmap!.Status == BeatmapStatus.Ranked || x.Beatmap!.Status == BeatmapStatus.Approved)
            .GroupBy(x => x.BeatmapId)
            .ToListAsync();

        _logger.LogInformation("Populating all scores pp - {Total} maps total", mapScores.Count);

        var populatedScores = 0;

        for (var i = 0; i < mapScores.Count; i += 500)
        {
            var batch = mapScores.Skip(i).Take(500).ToList();
            
            var queryBuilder = new ConcurrentBag<string>();
            await Parallel.ForEachAsync(batch, (mapGroup, _) =>
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

                        if (score.Pp != performanceAttributes.Total)
                        {
                            queryBuilder.Add($"UPDATE \"Scores\" SET \"Pp\" = {performanceAttributes.Total} WHERE \"Id\" = {score.Id};");
                            Interlocked.Increment(ref populatedScores);
                        }
                    }
                }

                return ValueTask.CompletedTask;
            });

            _logger.LogInformation("Populating all scores pp - saving batch of {Total} updates", queryBuilder.Count);

            if (!queryBuilder.IsEmpty)
            {
                await _databaseContext.Database.ExecuteSqlRawAsync(string.Join('\n', queryBuilder));
            }
        }

        _logger.LogInformation("Populating all scores pp done - {Total} affected scores!", populatedScores);

        return Ok(populatedScores);
    }

    [HttpPost("recalculatePlayerPp")]
    public async Task<IActionResult> PopulatePlayerPp()
    {
        if (!_authService.Authorize(HttpContext))
        {
            return Unauthorized();
        }

        _logger.LogInformation("Recalculating all player pp...");

        var players = await _databaseContext.Users.ToListAsync();
        foreach (var player in players)
        {
            var scores = await _databaseContext.Scores.AsNoTracking()
                .Where(x => x.Beatmap != null)
                .Include(x => x.Beatmap)
                .Select(x => new { x.UserId, BeatmapStatus = x.Beatmap!.Status, x.Pp, x.Accuracy })
                .Where(x => x.UserId == player.Id)
                .Where(x => x.BeatmapStatus == BeatmapStatus.Ranked || x.BeatmapStatus == BeatmapStatus.Approved)
                .OrderByDescending(x => x.Pp)
                .ToArrayAsync();

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

            // We want our accuracy to be normalized.
            if (scores.Length > 0)
            {
                // We want the percentage, not a factor in [0, 1], hence we divide 20 by 100.
                totalAccuracy *= 100.0 / (20 * (1 - Math.Pow(0.95, scores.Length)));
            }

            // handle floating point precision edge cases.
            totalAccuracy = Math.Clamp(totalAccuracy, 0, 100);

            player.TotalPp = totalPp;
            player.TotalAccuracy = totalAccuracy;
            _databaseContext.Users.Update(player);
        }

        await _databaseContext.SaveChangesAsync();

        _logger.LogInformation("Recalculating all player pp done!");

        return Ok();
    }

    private Mod[] GetMods(Ruleset ruleset, string[] modNames)
    {
        var availableMods = ruleset.CreateAllMods().ToList();
        var mods = new List<Mod>();

        foreach (var modString in modNames)
        {
            var newMod = availableMods.First(m => string.Equals(m.Acronym, modString, StringComparison.OrdinalIgnoreCase));
            if (newMod == null)
                throw new ArgumentException($"Invalid mod provided: {modString}");

            mods.Add(newMod);
        }

        return mods.ToArray();
    }
}
