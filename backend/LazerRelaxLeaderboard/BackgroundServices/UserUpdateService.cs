using LazerRelaxLeaderboard.Database;
using LazerRelaxLeaderboard.OsuApi.Interfaces;
using LazerRelaxLeaderboard.Services;
using Microsoft.EntityFrameworkCore;

namespace LazerRelaxLeaderboard.BackgroundServices
{
    public class UserUpdateService : BackgroundService
    {
        private readonly int _apiInterval;
        private readonly IOsuApiProvider _osuApiProvider;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly ILogger<LeaderboardUpdateService> _logger;
        public UserUpdateService(IOsuApiProvider osuApiProvider, IConfiguration configuration,
            ILogger<LeaderboardUpdateService> logger, IServiceScopeFactory serviceScopeFactory)
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

                    var users = await context.Users.AsNoTracking().Select(x=> x.Id).ToArrayAsync(cancellationToken: stoppingToken);

                    foreach (var userId in users)
                    {
                        var osuUser = await _osuApiProvider.GetUser(userId);
                        if (osuUser == null)
                        {
                            _logger.LogInformation("Nuking player {UserId}...", userId);

                            await context.Scores.Where(x => x.UserId == userId)
                                .ExecuteUpdateAsync(u => u.SetProperty(x => x.Hidden, true),
                                    cancellationToken: stoppingToken);

                            await ppService.RecalculatePlayersPp([userId]);
                            await ppService.RecalculateBestScores([userId]);
                        }
                        else
                        {
                            await context.Users.Where(x=> x.Id == userId)
                                .ExecuteUpdateAsync(u => u.SetProperty(x => x.Username, osuUser.Username).SetProperty(x => x.CountryCode, osuUser.CountryCode),
                                    cancellationToken: stoppingToken);
                        }

                        await Task.Delay(_apiInterval, stoppingToken);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "UserUpdateService failed!");
                }

                await Task.Delay(_apiInterval, stoppingToken);
            }
        }
    }
}
