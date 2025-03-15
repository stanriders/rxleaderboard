using LazerRelaxLeaderboard.Database;
using LazerRelaxLeaderboard.OsuApi.Interfaces;
using LazerRelaxLeaderboard.OsuApi.Models;
using LazerRelaxLeaderboard.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace LazerRelaxLeaderboard.Controllers;

[ApiController]
[Route("[controller]")]
[EnableRateLimiting("token")]
public class AdminController : ControllerBase
{
    private readonly IKeyAuthService _authService;
    private readonly DatabaseContext _databaseContext;
    private readonly ILogger<AdminController> _logger;
    private readonly IOsuApiProvider _osuApiProvider;
    private readonly IPpService _ppService;

    public AdminController(IKeyAuthService authService, IPpService ppService, DatabaseContext databaseContext,
        IOsuApiProvider osuApiProvider, ILogger<AdminController> logger)
    {
        _authService = authService;
        _ppService = ppService;
        _databaseContext = databaseContext;
        _osuApiProvider = osuApiProvider;
        _logger = logger;
    }

    [HttpPost("populateBeatmaps")]
    public async Task<IActionResult> PopulateBeatmaps()
    {
        if (!_authService.Authorize(HttpContext))
        {
            return Unauthorized();
        }

        await _ppService.PopulateStarRatings();

        return Ok();
    }

    [HttpPost("recalculateStarRatings")]
    public async Task<IActionResult> PopulateSr()
    {
        if (!_authService.Authorize(HttpContext))
        {
            return Unauthorized();
        }

        await _ppService.RecalculateStarRatings();

        return Ok();
    }

    [HttpPost("recalculatePp")]
    public async Task<IActionResult> PopulatePp()
    {
        if (!_authService.Authorize(HttpContext))
        {
            return Unauthorized();
        }

        await _ppService.PopulateScores(true);
        await _ppService.RecalculatePlayersPp();
        await _ppService.RecalculateBestScores();

        return Ok();
    }

    [HttpPost("recalculatePlayerPp")]
    public async Task<IActionResult> PopulatePlayerPp()
    {
        if (!_authService.Authorize(HttpContext))
        {
            return Unauthorized();
        }

        await _ppService.RecalculatePlayersPp();

        return Ok();
    }

    [HttpPost("populateScores")]
    public async Task<IActionResult> PopulateScoreData()
    {
        if (!_authService.Authorize(HttpContext))
        {
            return Unauthorized();
        }

        _logger.LogInformation("Started populating all scores...");

        var rankedScores = await _databaseContext.Scores.AsNoTracking()
            .Where(x => x.Pp != null)
            .OrderByDescending(x => x.Pp)
            .Select(x => x.Id)
            .ToArrayAsync();

        for (var i = 0; i < rankedScores.Length; i += 50)
        {
            _logger.LogInformation("Starting score population batch {Count}/{Total}", i, rankedScores.Length);

            var batch = rankedScores.Skip(i).Take(50).ToArray();
            foreach (var scoreId in batch)
            {
                var dbScore = await _databaseContext.Scores.FindAsync(scoreId);
                if (dbScore == null)
                {
                    _logger.LogWarning("Score {Id} doesn't exist in the database???", scoreId);
                    continue;
                }

                var osuScore = await _osuApiProvider.GetScore(scoreId);
                await Task.Delay(500);

                if (osuScore == null)
                {
                    _logger.LogWarning("Score {Id} doesn't exist in osu! api", scoreId);
                    _databaseContext.Scores.Remove(dbScore);
                    continue;
                }

                dbScore.LegacySliderEndMisses = osuScore.Statistics.LegacySliderEndMisses;
                dbScore.SliderTickMisses = osuScore.Statistics.SliderTickMisses;

                _databaseContext.Scores.Update(dbScore);
            }

            await _databaseContext.SaveChangesAsync();
        }

        _logger.LogInformation("Finished populating all scores...");

        return Ok();
    }

    [HttpPost("recalculateBestScores")]
    public async Task<IActionResult> RecalculateBests()
    {
        if (!_authService.Authorize(HttpContext))
        {
            return Unauthorized();
        }

        await _ppService.RecalculateBestScores();

        return Ok();
    }

    [HttpPost("makeSureMapStatusesMakeSense")]
    public async Task<IActionResult> Bruh()
    {
        if (!_authService.Authorize(HttpContext))
        {
            return Unauthorized();
        }

        var potentiallyBrokenMaps = await _databaseContext.Beatmaps.Where(x =>
                x.Status != BeatmapStatus.Ranked &&
                x.Status != BeatmapStatus.Approved &&
                x.Status != BeatmapStatus.Loved)
            .ToListAsync();

        _logger.LogInformation("Starting map statuses for {Count} maps fixup...", potentiallyBrokenMaps.Count);

        for (var i = 0; i < potentiallyBrokenMaps.Count; i += 100)
        {
            foreach (var beatmap in potentiallyBrokenMaps.Skip(i).Take(100))
            {
                var osuBeatmap = await _osuApiProvider.GetBeatmap(beatmap.Id);
                if (osuBeatmap == null)
                {
                    _databaseContext.Beatmaps.Remove(beatmap);

                    continue;
                }

                beatmap.Status = osuBeatmap.Status;
                _databaseContext.Beatmaps.Update(beatmap);

                await Task.Delay(500);
            }

            _logger.LogInformation("Saving map statuses for 100 maps ({Current}/{Total})...", i, potentiallyBrokenMaps.Count);
            await _databaseContext.SaveChangesAsync();
        }

        var affected = await _databaseContext.SaveChangesAsync();
        return Ok(affected);
    }

    [HttpPost("nuke/{id}")]
    public async Task<IActionResult> NukePlayer(int id)
    {
        if (!_authService.Authorize(HttpContext))
        {
            return Unauthorized();
        }

        var player = await _databaseContext.Users.FindAsync(id);
        if (player == null)
        {
            return NotFound("Player doesn't exist");
        }

        _logger.LogInformation("Nuking player {Username}...", player.Username);

        var scores = await _databaseContext.Scores.Where(x => x.UserId == id).ToListAsync();
        foreach (var score in scores)
        {
            score.Hidden = true;
            _databaseContext.Scores.Update(score);
        }

        await _databaseContext.SaveChangesAsync();

        await _ppService.RecalculatePlayersPp([id]);
        await _ppService.RecalculateBestScores([id]);

        return Ok($"Nuked {player.Username}");
    }

    [HttpGet("hidden")]
    public async Task<IActionResult> GetHiddenScores()
    {
        if (!_authService.Authorize(HttpContext))
        {
            return Unauthorized();
        }

        return Ok(await _databaseContext.Scores.Where(x => x.Hidden).ToListAsync());
    }
}
