using LazerRelaxLeaderboard.Database;
using LazerRelaxLeaderboard.OsuApi.Interfaces;
using LazerRelaxLeaderboard.OsuApi.Models;
using LazerRelaxLeaderboard.Services;
using Microsoft.EntityFrameworkCore;
using Beatmap = LazerRelaxLeaderboard.Database.Models.Beatmap;
using Score = LazerRelaxLeaderboard.Database.Models.Score;
using User = LazerRelaxLeaderboard.Database.Models.User;

namespace LazerRelaxLeaderboard.BackgroundServices;

public class LeaderboardUpdateService : BackgroundService
{
    private readonly int _apiInterval;
    private readonly string _cachePath;
    private readonly bool _enableProcessing;
    private readonly ILogger<LeaderboardUpdateService> _logger;
    private readonly IOsuApiProvider _osuApiProvider;
    private readonly int _queryInterval;
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public LeaderboardUpdateService(IOsuApiProvider osuApiProvider, IConfiguration configuration,
        ILogger<LeaderboardUpdateService> logger, IServiceScopeFactory serviceScopeFactory)
    {
        _osuApiProvider = osuApiProvider;
        _logger = logger;
        _serviceScopeFactory = serviceScopeFactory;
        _apiInterval = int.Parse(configuration["APIQueryInterval"]!);
        _queryInterval = int.Parse(configuration["ScoreQueryInterval"]!);
        _cachePath = configuration["BeatmapCachePath"]!;
        _enableProcessing = bool.Parse(configuration["EnableScoreProcessing"]!);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_enableProcessing)
        {
            _logger.LogWarning("LeaderboardUpdateService is disabled");

            return;
        }

        var currentCursor = 0L;

        var cursorResponse = await _osuApiProvider.GetScores(null);
        if (cursorResponse == null)
        {
            _logger.LogWarning("Couldn't get current max score id!");
        }
        else
        {
            // catch up on the potentially missed scores while we were offline
            // 200k scores is ~an hour of scores which is getting processed in ~3.5 minutes
            currentCursor = cursorResponse.Scores.OrderByDescending(x => x.Id).First().Id - 200_000;
        }

        await Task.Delay(_apiInterval, stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            var interval = _queryInterval;

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

                var currentMaxScoreId = await context.Scores.AsNoTracking()
                    .Select(x => x.Id)
                    .OrderByDescending(x => x)
                    .FirstAsync(stoppingToken);

                if (currentCursor == 0)
                {
                    currentCursor = currentMaxScoreId;
                }

                if (currentCursor < currentMaxScoreId)
                {
                    // speed up the catching up process
                    interval = _queryInterval / 5;
                }

                var scoreResponse = await _osuApiProvider.GetScores(currentCursor);
                if (scoreResponse == null)
                {
                    _logger.LogError("Score query failed!");
                    await Task.Delay(interval, stoppingToken);
                    continue;
                }

                _logger.LogInformation("Got a new score batch of {Count}, cursor {Cursor}", scoreResponse.Scores.Count,
                    currentCursor);

                if (scoreResponse.Scores.Count == 0)
                {
                    await Task.Delay(interval, stoppingToken);
                    continue;
                }

                var affectedPlayers = await ProcessScores(scoreResponse.Scores, context);

                // somehow database seem to have issues keeping up and fails to properly calculate best scores, so we wait for a bit
                await Task.Delay(_apiInterval, stoppingToken);

                await ppService.PopulateScores();

                if (affectedPlayers.Count > 0)
                {
                    await ppService.RecalculateBestScores(affectedPlayers);
                    await ppService.RecalculatePlayersPp(affectedPlayers);
                }

                await ppService.PopulateStarRatings();

                currentCursor = scoreResponse.Scores.OrderByDescending(x => x.Id).First().Id;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "LeaderboardUpdateService failed!");
            }

            await Task.Delay(interval, stoppingToken);
        }
    }

    private async Task<List<int>> ProcessScores(List<OsuApi.Models.Score> scores, DatabaseContext context)
    {
        var affectedPlayers = new List<int>();

        var relevantScores = scores.Where(x => x.Mods.Any(m => m.Acronym == "RX"))
            .Where(x => x.Mode == Mode.Osu)
            .Where(x => Utils.CheckAllowedMods(x.Mods) && Utils.CheckAllowedModSettings(x.Mods));

        foreach (var score in relevantScores)
        {
            try
            {
                if (await context.Scores.AnyAsync(x => x.Id == score.Id))
                {
                    continue;
                }

                var dbBeatmap = await context.Beatmaps.FindAsync(score.BeatmapId);
                if (dbBeatmap == null)
                {
                    var osuBeatmap = await _osuApiProvider.GetBeatmap(score.BeatmapId);
                    if (osuBeatmap == null)
                    {
                        _logger.LogWarning("Beatmap {Id} was not found on osu!API", score.BeatmapId);

                        // wait until querying api again
                        await Task.Delay(_apiInterval);
                        continue;
                    }

                    if (osuBeatmap.Status is not BeatmapStatus.Ranked and
                        not BeatmapStatus.Approved and
                        not BeatmapStatus.Loved)
                    {
                        await Task.Delay(_apiInterval);
                        continue;
                    }

                    if (osuBeatmap.Mode != Mode.Osu)
                    {
                        _logger.LogWarning("Beatmap {Id} mode is not osu! ({Mode})", score.BeatmapId, osuBeatmap.Mode);

                        await Task.Delay(_apiInterval);
                        continue;
                    }

                    // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
                    // beatmapsets should never be null, however SOMEHOW they sometimes are
                    if (osuBeatmap.BeatmapSet == null)
                    {
                        _logger.LogWarning("Beatmap {Id} is broken", score.BeatmapId);

                        await Task.Delay(_apiInterval);

                        continue;
                    }

                    var mapPath = $"{_cachePath}/{osuBeatmap.Id}.osu";
                    if (!File.Exists(mapPath))
                    {
                        if (!await _osuApiProvider.DownloadMap(osuBeatmap.Id, mapPath))
                        {
                            _logger.LogWarning("Couldn't download beatmap {Id}!", score.BeatmapId);

                            await Task.Delay(_apiInterval);
                            continue;
                        }
                    }

                    context.Beatmaps.Add(new Beatmap
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

                    // wait until querying api again
                    await Task.Delay(_apiInterval);
                }

                var user = await context.Users.FindAsync(score.UserId);
                if (user == null)
                {
                    var osuUser = await _osuApiProvider.GetUser(score.UserId);
                    if (osuUser == null)
                    {
                        _logger.LogWarning("User {Id} was not found on osu!API", score.UserId);
                        continue;
                    }

                    context.Users.Add(new User
                    {
                        Id = osuUser.Id,
                        CountryCode = osuUser.CountryCode,
                        UpdatedAt = DateTime.UtcNow,
                        Username = osuUser.Username
                    });
                }

                context.Scores.Add(new Score
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
                    UserId = score.UserId,
                    IsBest = false
                });

                if (!affectedPlayers.Contains(score.UserId))
                {
                    affectedPlayers.Add(score.UserId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process score {ScoreId}", score.Id);
            }
        }

        await context.SaveChangesAsync();

        return affectedPlayers;
    }
}
