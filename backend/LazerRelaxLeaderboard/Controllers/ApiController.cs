using LazerRelaxLeaderboard.Contracts;
using LazerRelaxLeaderboard.Database;
using LazerRelaxLeaderboard.Database.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LazerRelaxLeaderboard.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ApiController : ControllerBase
    {
        private readonly DatabaseContext _databaseContext;
        private readonly int _apiRequestInterval;

        public ApiController(DatabaseContext databaseContext, IConfiguration configuration)
        {
            _databaseContext = databaseContext;
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
        public async Task<PlayersResult> GetTopPlayers(int page = 1)
        {
            const int take = 50;

            return new PlayersResult()
            {
                Players = await _databaseContext.Users.AsNoTracking()
                    .Where(x => x.TotalPp != null)
                    .OrderByDescending(x => x.TotalPp)
                    .Skip((page - 1) * take)
                    .Take(take)
                    .ToListAsync(),
                Total = await _databaseContext.Users.AsNoTracking().CountAsync(x => x.TotalPp != null)
            };
        }

        [HttpGet("/players/{id}")]
        public async Task<User?> GetPlayer(int id)
        {
            return await _databaseContext.Users.AsNoTracking()
                .Where(x => x.Id == id)
                .FirstOrDefaultAsync();
        }

        [HttpGet("/players/search/{query}")]
        public async Task<List<User>> GetPlayer(string query)
        {
            if (int.TryParse(query, out var id))
            {
                return await _databaseContext.Users.AsNoTracking()
                    .Where(x => x.Id == id)
                    .ToListAsync();
            }

            return await _databaseContext.Users.AsNoTracking()
                .Where(x => x.Username.ToUpper().StartsWith(query.ToUpper()))
                .ToListAsync();
        }

        [HttpGet("/players/{id}/scores")]
        public async Task<List<Score>> GetPlayerScores(int id)
        {
            return await _databaseContext.Scores.AsNoTracking()
                .Where(x => x.UserId == id && x.Pp != null)
                .Include(x => x.Beatmap)
                .OrderByDescending(x => x.Pp)
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

        [HttpGet("/stats")]
        public async Task<StatsResponse> GetStats()
        {
            var beatmaps = await _databaseContext.Beatmaps.CountAsync();
            const int modCombos = 8;

            return new StatsResponse
            {
                BeatmapsTotal = beatmaps,
                ScoresTotal = await _databaseContext.Scores.CountAsync(),
                UsersTotal = await _databaseContext.Users.CountAsync(),
                UpdateRunLengthEstimate = (beatmaps * modCombos) * (_apiRequestInterval / 1000.0) / 60.0 / 60.0 / 24.0
            };
        }
    }
}
