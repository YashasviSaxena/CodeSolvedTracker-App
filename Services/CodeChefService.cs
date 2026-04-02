using CodeSolvedTracker.Models;

namespace CodeSolvedTracker.Services
{
    public class CodeChefService
    {
        public async Task<PlatformStatsDto> GetUserStatsAsync(string username)
        {
            // Dummy data (replace with scraping later)
            return await Task.FromResult(new PlatformStatsDto
            {
                Platform = "CodeChef",
                Username = username,
                Easy = 40,
                Medium = 35,
                Hard = 25,
                TotalSolved = 100
            });
        }
    }
}