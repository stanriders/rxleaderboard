﻿using LazerRelaxLeaderboard.Database;
using LazerRelaxLeaderboard.OsuApi.Interfaces;
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
    private readonly IPpService _ppService;
    private readonly DatabaseContext _databaseContext;
    private readonly IOsuApiProvider _osuApiProvider;
    private readonly ILogger<AdminController> _logger;

    public AdminController(IKeyAuthService authService, IPpService ppService, DatabaseContext databaseContext, IOsuApiProvider osuApiProvider, ILogger<AdminController> logger)
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
            .OrderByDescending(x=> x.Pp)
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
}
