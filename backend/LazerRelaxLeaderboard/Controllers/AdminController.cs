using LazerRelaxLeaderboard.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace LazerRelaxLeaderboard.Controllers;

[ApiController]
[Route("[controller]")]
[EnableRateLimiting("token")]
public class AdminController : ControllerBase
{
    private readonly IKeyAuthService _authService;
    private readonly IPpService _ppService;

    public AdminController(IKeyAuthService authService, IPpService ppService)
    {
        _authService = authService;
        _ppService = ppService;
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
}
