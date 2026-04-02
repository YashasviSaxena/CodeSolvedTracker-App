using CodeSolvedTracker.Models;

namespace CodeSolvedTracker.Services
{
    public class HackerRankService
    {
        public async Task<PlatformStatsDto> GetUserStatsAsync(string username)
        {
            // Dummy data (replace with scraping later)
            return await Task.FromResult(new PlatformStatsDto
            {
                Platform = "HackerRank",
                Username = username,
                Easy = 30,
                Medium = 50,
                Hard = 20,
                TotalSolved = 100
            });
        }
    }
}