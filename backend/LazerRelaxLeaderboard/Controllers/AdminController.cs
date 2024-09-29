using LazerRelaxLeaderboard.Database;
using LazerRelaxLeaderboard.OsuApi.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using osu.Game.Beatmaps;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Osu;
using osu.Game.Rulesets.Osu.Mods;
using osu.Game.Rulesets.Scoring;
using osu.Game.Scoring;

namespace LazerRelaxLeaderboard.Controllers
{
#if DEBUG
    [ApiController]
    [Route("[controller]")]
    public class AdminController : ControllerBase
    {
        private readonly IOsuApiProvider _osuApiProvider;
        private readonly DatabaseContext _databaseContext;
        private readonly ILogger<AdminController> _logger;
        private readonly string _cachePath;
        private readonly int _interval;

        public AdminController(IOsuApiProvider osuApiProvider, DatabaseContext databaseContext, IConfiguration configuration, ILogger<AdminController> logger)
        {
            _osuApiProvider = osuApiProvider;
            _databaseContext = databaseContext;
            _logger = logger;
            _interval = int.Parse(configuration["ScoreQueryInterval"]!);
            _cachePath = configuration["BeatmapCachePath"]!;
        }

        [HttpGet("populate")]
        public async Task<IActionResult> StartScorePopulation()
        {
            var maps = (await System.IO.File.ReadAllLinesAsync($"{_cachePath}/../output.txt")).Select(x=> int.Parse(x[..^4])).ToArray();
            var parsedMaps = await _databaseContext.Beatmaps.AsNoTracking().Select(m => m.Id).ToArrayAsync();
            var unparsedMaps = maps.Where(x => !parsedMaps.Contains(x)).ToArray();

            //for (var i = 0; i < 500; i++)
            {
                //_logger.LogInformation("Populating {Current}/{Total}", i, unparsedMaps.Length);
                //Console.WriteLine($"Populating {i}/{unparsedMaps.Length}");
                var mapId = 1256809;//unparsedMaps[Random.Shared.Next(unparsedMaps.Length)];

                var allowedMods = new[] { "HD", "DT", "HR" };
                var modCombos = CreateCombinations(0, Array.Empty<string>(), allowedMods);
                modCombos.Add(new [] {string.Empty});

                foreach (var modCombo in modCombos)
                {
                    var scores = await _osuApiProvider.GetScores(mapId, modCombo);
                    if (scores == null)
                    {
                        //i--;
                        await Task.Delay(1000);
                        continue;
                    }

                    var filteredScores = scores.Scores.Where(s => s.Mods.All(m => m.Settings.Count == 0));
                    foreach (var score in filteredScores)
                    {
                        if (await _databaseContext.Scores.FindAsync(score.Id) != null)
                            continue;

                        var user = await _databaseContext.Users.FindAsync(score.User.Id);
                        if (user != null)
                        {
                            user.CountryCode = score.User.CountryCode;
                            user.UpdatedAt = DateTime.UtcNow;
                            user.Username = score.User.Username;
                        }
                        else
                        {
                            await _databaseContext.Users.AddAsync(new Database.Models.User
                            {
                                Id = score.User.Id,
                                CountryCode = score.User.CountryCode,
                                UpdatedAt = DateTime.UtcNow,
                                Username = score.User.Username
                            });
                        }
                        
                        await _databaseContext.Scores.AddAsync(new Database.Models.Score
                        {
                            Id = score.Id,
                            Accuracy = score.Accuracy,
                            BeatmapId = score.BeatmapId,
                            Combo = score.Combo,
                            Count100 = score.Statistics.Count100,
                            Count300 = score.Statistics.Count300,
                            Count50 = score.Statistics.Count50,
                            CountMiss = score.Statistics.CountMiss,
                            Date = score.Date,
                            Grade = score.Grade,
                            Mods = score.Mods.Select(x => x.Acronym).ToArray(),
                            TotalScore = score.TotalScore,
                            UserId = score.User.Id,
                        });

                    }

                    await Task.Delay(_interval);
                }

                await _databaseContext.SaveChangesAsync();
            }

            return Ok(unparsedMaps.Length);
        }

        [HttpPost("populateBeatmaps")]
        public async Task<IActionResult> PopulateBeatmaps()
        {
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
                        StarRatingNormal = osuBeatmap.StarRating
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

        [HttpPost("populatePp")]
        public async Task<IActionResult> PopulatePp()
        {
            var mapScores = await _databaseContext.Scores
                .Where(x=> x.Pp == null)
                .GroupBy(x=> x.BeatmapId)
                .ToListAsync();

            var populatedScores = 0;

            foreach (var mapGroup in mapScores)
            {
                var mapPath = $"{_cachePath}/{mapGroup.Key}.osu";

                var workingBeatmap = new FlatWorkingBeatmap(mapPath);

                var ruleset = new OsuRuleset();
                var difficultyCalculator = ruleset.CreateDifficultyCalculator(workingBeatmap);

                foreach (var modsGroup in mapGroup.GroupBy(x=> x.Mods))
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

                        var performanceAttributes = performanceCalculator.Calculate(scoreInfo, difficultyAttributes);

                        score.Pp = performanceAttributes.Total;
                        _databaseContext.Scores.Update(score);
                        populatedScores++;
                    }
                }
            }

            await _databaseContext.SaveChangesAsync();

            return Ok(populatedScores);
        }

        [HttpPost("populatePlayerPp")]
        public async Task<IActionResult> PopulatePlayerPp()
        {
            var players = await _databaseContext.Users.ToListAsync();
            foreach (var player in players)
            {
                var scores = await _databaseContext.Scores.AsNoTracking()
                    .Where(x => x.UserId == player.Id)
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

            await  _databaseContext.SaveChangesAsync();

            return Ok();
        }

        private osu.Game.Rulesets.Mods.Mod[] GetMods(Ruleset ruleset, string[] modNames)
        {
            var availableMods = ruleset.CreateAllMods().ToList();
            var mods = new List<osu.Game.Rulesets.Mods.Mod>();

            foreach (var modString in modNames)
            {
                var newMod = availableMods.First(m => string.Equals(m.Acronym, modString, StringComparison.OrdinalIgnoreCase));
                if (newMod == null)
                    throw new ArgumentException($"Invalid mod provided: {modString}");

                mods.Add(newMod);
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
    }
#endif
}
