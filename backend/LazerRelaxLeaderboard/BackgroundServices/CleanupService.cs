using LazerRelaxLeaderboard.Database;
using LazerRelaxLeaderboard.Services;

namespace LazerRelaxLeaderboard.BackgroundServices
{
    public class CleanupService : BackgroundService
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly ILogger<CleanupService> _logger;
        private readonly int _interval;

        public CleanupService(IServiceScopeFactory serviceScopeFactory, ILogger<CleanupService> logger, IConfiguration configuration)
        {
            _serviceScopeFactory = serviceScopeFactory;
            _logger = logger;
            _interval = int.Parse(configuration["CleanupInterval"]!);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Running cleanup job...");

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

                    await ppService.PopulateStarRatings();
                    await ppService.PopulateScores();
                    await ppService.CleanupScores();
                    await ppService.RecalculateBestScores();
                    await ppService.RecalculatePlayersPp();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "CleanupService failed!");
                }

                await Task.Delay(_interval, stoppingToken);
            }
        }
    }
}
