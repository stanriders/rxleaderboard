using LazerRelaxLeaderboard.Contracts;
using LazerRelaxLeaderboard.Database;
using LazerRelaxLeaderboard.OsuApi.Interfaces;
using LazerRelaxLeaderboard.OsuApi.Models;
using LazerRelaxLeaderboard.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Beatmap = LazerRelaxLeaderboard.Database.Models.Beatmap;
using Score = LazerRelaxLeaderboard.Database.Models.Score;
using User = LazerRelaxLeaderboard.Database.Models.User;

namespace LazerRelaxLeaderboard.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ApiController : ControllerBase
    {
        private readonly DatabaseContext _databaseContext;
        private readonly IOsuApiProvider _osuApiProvider;
        private readonly IPpService _ppService;
        private readonly int _apiRequestInterval;

        public ApiController(DatabaseContext databaseContext, IConfiguration configuration, IOsuApiProvider osuApiProvider, IPpService ppService)
        {
            _databaseContext = databaseContext;
            _osuApiProvider = osuApiProvider;
            _ppService = ppService;
            _apiRequestInterval = int.Parse(configuration["ScoreQueryInterval"]!);
        }

        [HttpGet("/scores")]
        public async Task<List<Score>> GetTopScores()
        {
            return await _databaseContext.Scores.AsNoTracking()
                .Where(x => x.Pp != null)
                .Include(x => x.Beatmap)
                .Include(x => x.User)
                .OrderByDescending(x => x.Pp)
                .Take(50)
                .ToListAsync();
        }

        [HttpGet("/players")]
        public async Task<PlayersResult> GetTopPlayers(int page = 1, string? search = null)
        {
            const int take = 50;

            var query = _databaseContext.Users.AsNoTracking();

            if (!string.IsNullOrEmpty(search))
            {
                if (int.TryParse(search, out var id))
                {
                    query = query.Where(x => x.Id == id);
                }
                else
                {
                    query = query.Where(x => x.Username.ToUpper().StartsWith(search.ToUpper()));
                }
            }

            var result = await query
                .Where(x => x.TotalPp != null)
                .OrderByDescending(x => x.TotalPp)
                .Skip((page - 1) * take)
                .Take(take)
                .ToListAsync();

            return new PlayersResult
            {
                Players = result,
                Total = await query.CountAsync(x => x.TotalPp != null)
            };
        }

        [HttpGet("/players/{id}")]
        public async Task<PlayersDataResponse?> GetPlayer(int id)
        {
            var user = await _databaseContext.Users.AsNoTracking()
                .Where(x => x.Id == id)
                .FirstOrDefaultAsync();

            if (user != null)
            {
                int? rank = null;
                if (user.TotalPp != null)
                {
                    // todo: how expensive is it?
                    rank = await _databaseContext.Database
                        .SqlQuery<int>($"select x.row_number as \"Value\" from (SELECT \"Id\", ROW_NUMBER() OVER(order by \"TotalPp\" desc) FROM \"Users\" where \"TotalPp\" is not null) x WHERE x.\"Id\" = {user.Id}")
                        .SingleOrDefaultAsync();
                }

                return new PlayersDataResponse
                {
                    Id = user.Id,
                    Rank = rank,
                    CountryCode = user.CountryCode,
                    TotalAccuracy = user.TotalAccuracy,
                    Username = user.Username,
                    UpdatedAt = user.UpdatedAt,
                    TotalPp = user.TotalPp
                };
            }
            return null;
        }

        [HttpGet("/players/{id}/scores")]
        public async Task<List<Score>> GetPlayerScores(int id)
        {
            return await _databaseContext.Scores.AsNoTracking()
                .Where(x => x.UserId == id)
                .Include(x => x.Beatmap)
                .OrderByDescending(x => x.Pp ?? double.MinValue)
                .Take(100)
                .ToListAsync();
        }

        [HttpGet("/beatmaps/{id}")]
        public async Task<Beatmap?> GetBeatmap(int id)
        {
            return await _databaseContext.Beatmaps.AsNoTracking()
                .Where(x => x.Id == id)
                .FirstOrDefaultAsync();
        }

        [HttpGet("/beatmaps/{id}/scores")]
        public async Task<List<Score>> GetBeatmapScores(int id)
        {
            return await _databaseContext.Scores.AsNoTracking()
                .Where(x => x.BeatmapId == id)
                .Include(x => x.User)
                .OrderByDescending(x => x.Pp)
                .Take(100)
                .ToListAsync();
        }

        [HttpPost("/scores/add")]
        [EnableRateLimiting("token")]
        public async Task<IActionResult> AddScore(long id)
        {
            if (await _databaseContext.Scores.AnyAsync(x => x.Id == id))
            {
                return BadRequest("Score already exists");
            }

            var osuScore = await _osuApiProvider.GetScore(id);
            if (osuScore == null)
            {
                return BadRequest("Invalid score ID");
            }

            var allowedMods = new[] { "HD", "DT", "HR", "CL", "MR", "TC", "RX" };

            if (!osuScore.Mods.Any(x=> x.Acronym == "RX"))
            {
                return BadRequest("Score doesn't have RX enabled");
            }

            if (!osuScore.Mods.All(x => allowedMods.Contains(x.Acronym)))
            {
                return BadRequest("Score has unsupported mods");
            }

            var dbBeatmap = await _databaseContext.Beatmaps.FindAsync(osuScore.BeatmapId);
            if (dbBeatmap == null)
            {
                var osuBeatmap = await _osuApiProvider.GetBeatmap(osuScore.BeatmapId);
                if (osuBeatmap == null)
                {
                    return BadRequest("Score has invalid map ID");
                }

                if (osuBeatmap.Mode != OsuApi.Models.Mode.Osu)
                {
                    return BadRequest("Unsupported gamemode");
                }

                if (osuBeatmap.Status != BeatmapStatus.Ranked &&
                    osuBeatmap.Status != BeatmapStatus.Approved &&
                    osuBeatmap.Status != BeatmapStatus.Loved)
                {
                    return BadRequest("Only scores on ranked/loved maps are supported");
                }

                dbBeatmap = new Beatmap
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
                    ScoresUpdatedOn = DateTime.MinValue
                };
                await _databaseContext.Beatmaps.AddAsync(dbBeatmap);

                await _databaseContext.SaveChangesAsync();
            }

            var user = await _databaseContext.Users.FindAsync(osuScore.User.Id);
            if (user != null)
            {
                user.CountryCode = osuScore.User.CountryCode;
                user.UpdatedAt = DateTime.UtcNow;
                user.Username = osuScore.User.Username;
            }
            else
            {
                await _databaseContext.Users.AddAsync(new User
                {
                    Id = osuScore.User.Id,
                    CountryCode = osuScore.User.CountryCode,
                    UpdatedAt = DateTime.UtcNow,
                    Username = osuScore.User.Username
                });
            }

            await _databaseContext.Scores.AddAsync(new Score
            {
                Id = osuScore.Id,
                Accuracy = osuScore.Accuracy,
                BeatmapId = osuScore.BeatmapId,
                Combo = osuScore.Combo,
                Count100 = osuScore.Statistics.Count100,
                Count300 = osuScore.Statistics.Count300,
                Count50 = osuScore.Statistics.Count50,
                CountMiss = osuScore.Statistics.CountMiss,
                SliderEnds = osuScore.Statistics.SliderEnds,
                SliderTicks = osuScore.Statistics.SliderTicks,
                SpinnerBonus = osuScore.Statistics.SpinnerBonus,
                SpinnerSpins = osuScore.Statistics.SpinnerSpins,
                LegacySliderEnds = osuScore.Statistics.LegacySliderEnds,
                Date = osuScore.Date,
                Grade = osuScore.Grade,
                Mods = osuScore.Mods.Select(Utils.ModToString).ToArray(),
                TotalScore = osuScore.TotalScore,
                UserId = osuScore.User.Id,
            });

            await _databaseContext.SaveChangesAsync();

            // loved beatmaps dont affect pp
            if (dbBeatmap.Status != BeatmapStatus.Loved)
            {
                await _ppService.PopulateScorePp(id);
                await _ppService.RecalculatePlayerPp(osuScore.User.Id);
            }

            return Ok(await _databaseContext.Scores.FindAsync(id));
        }

        [HttpGet("/stats")]
        public async Task<StatsResponse> GetStats()
        {
            var beatmaps = await _databaseContext.Beatmaps.CountAsync();

            var allowedMods = new[] { "HD", "DT", "HR" };
            var modCombos = Utils.CreateCombinations(0, Array.Empty<string>(), allowedMods);

            var queries = modCombos.Count + 1; // mod combos + beatmap request

            return new StatsResponse
            {
                BeatmapsTotal = beatmaps,
                ScoresTotal = await _databaseContext.Scores.CountAsync(),
                UsersTotal = await _databaseContext.Users.CountAsync(),
                UpdateRunLengthEstimate = (beatmaps * queries) * (_apiRequestInterval / 1000.0) / 60.0 / 60.0 / 24.0
            };
        }
    }
}
