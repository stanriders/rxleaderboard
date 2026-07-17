using LazerRelaxLeaderboard.Database;
using LazerRelaxLeaderboard.OsuApi.Interfaces;
using LazerRelaxLeaderboard.Services;
using Microsoft.EntityFrameworkCore;

namespace LazerRelaxLeaderboard.BackgroundServices
{
    public class TopUserUpdateService : BackgroundService
    {
        private readonly int _apiInterval;
        private readonly IOsuApiProvider _osuApiProvider;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly ILogger<TopUserUpdateService> _logger;

        public TopUserUpdateService(IOsuApiProvider osuApiProvider, IConfiguration configuration,
            ILogger<TopUserUpdateService> logger, IServiceScopeFactory serviceScopeFactory)
        {
            _osuApiProvider = osuApiProvider;
            _logger = logger;
            _serviceScopeFactory = serviceScopeFactory;
            _apiInterval = int.Parse(configuration["APIQueryInterval"]!) * 2;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
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

                var users = await context.Users.AsNoTracking()
                    .OrderBy(x => x.TotalPp ?? double.MaxValue)
                    .Where(x=> x.TotalPp > 1000)
                    .Select(x => x.Id)
                    .ToArrayAsync(cancellationToken: stoppingToken);

                _logger.LogInformation("Updating {Count} top users...", users.Length);

                const int max_users = 50;

                for (var i = 0; i < users.Length; i += max_users)
                {
                    var usersSlice = users.Take(max_users).Skip(i).ToArray();

                    try
                    {
                        var osuUsers = await _osuApiProvider.GetUsers(usersSlice);
                        if (osuUsers == null)
                        {
                            continue;
                        }

                        foreach (var osuUser in osuUsers.Users)
                        {
                            await context.Users.Where(x => x.Id == osuUser.Id)
                                .ExecuteUpdateAsync(
                                    u => u.SetProperty(x => x.Username, osuUser.Username)
                                        .SetProperty(x => x.CountryCode, osuUser.CountryCode)
                                        .SetProperty(x => x.UpdatedAt, DateTime.UtcNow),
                                    cancellationToken: stoppingToken);

                            var hiddenScores = await context.Scores
                                .Where(x => x.UserId == osuUser.Id && x.Hidden && !x.Deleted).ToArrayAsync(stoppingToken);
                            if (hiddenScores.Length > 0)
                            {
                                var anyUnhiddenScores = false;

                                _logger.LogInformation("Unnuking player {UserId} ({Count} hidden scores)...", osuUser.Id,
                                    hiddenScores.Length);

                                // user has hidden scores, but exists on the API - check if they got wiped
                                foreach (var score in hiddenScores)
                                {
                                    var osuScore = await _osuApiProvider.GetScore(score.Id);
                                    if (osuScore != null)
                                    {
                                        // unhide scores that exist again
                                        score.Hidden = false;
                                        anyUnhiddenScores = true;
                                        _logger.LogInformation("Unhiding score {ScoreId}...", score.Id);
                                    }
                                    else
                                    {
                                        // score still doesn't exist, nuke it for good
                                        score.Hidden = true;
                                        score.Deleted = true;
                                    }

                                    await Task.Delay(_apiInterval, stoppingToken);
                                }

                                await context.SaveChangesAsync(stoppingToken);

                                if (anyUnhiddenScores)
                                {
                                    await ppService.RecalculatePlayersPp([osuUser.Id]);
                                    await ppService.RecalculateBestScores([osuUser.Id]);
                                }
                            }
                        }

                        foreach (var missingPlayer in usersSlice.Where(x => osuUsers.Users.All(o => o.Id != x)))
                        {
                            if (await context.Scores.AnyAsync(x => x.UserId == missingPlayer && !x.Hidden,
                                    cancellationToken: stoppingToken))
                            {
                                _logger.LogInformation("Nuking player {UserId}...", missingPlayer);

                                await context.Scores.Where(x => x.UserId == missingPlayer)
                                    .ExecuteUpdateAsync(u => u.SetProperty(x => x.Hidden, true),
                                        cancellationToken: stoppingToken);

                                await ppService.RecalculatePlayersPp([missingPlayer]);
                                await ppService.RecalculateBestScores([missingPlayer]);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "TopUserUpdateService failed! {Message}", ex.Message);
                    }

                    await Task.Delay(_apiInterval * 2, stoppingToken);
                }

                _logger.LogInformation("Finished updating top users");

                await Task.Delay(_apiInterval, stoppingToken);
            }
        }
    }
}
