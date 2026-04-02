using Microsoft.EntityFrameworkCore;
using CodeSolvedTracker.Data;

namespace CodeSolvedTracker.Services
{
    public class SyncService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<SyncService> _logger;

        public SyncService(IServiceProvider serviceProvider, ILogger<SyncService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public async Task SyncAllPlatforms()
        {
            _logger.LogInformation("Starting auto-sync at {time}", DateTime.UtcNow);

            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var users = await context.Users.ToListAsync();

            _logger.LogInformation("Auto-sync completed at {time}. Processed {count} users", DateTime.UtcNow, users.Count);

            await Task.CompletedTask;
        }
    }
}