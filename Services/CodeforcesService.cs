using System.Text.Json;
using CodeSolvedTracker.Models;

namespace CodeSolvedTracker.Services
{
    public class CodeforcesService
    {
        private readonly HttpClient _http = new HttpClient();

        public async Task<PlatformStatsDto> GetUserStatsAsync(string username)
        {
            var url = $"https://codeforces.com/api/user.status?handle={username}";
            var response = await _http.GetStringAsync(url);

            var jsonDoc = JsonDocument.Parse(response);
            var submissions = jsonDoc.RootElement.GetProperty("result");

            int easy = 0, medium = 0, hard = 0;

            foreach (var sub in submissions.EnumerateArray())
            {
                int rating = 0;

                if (sub.TryGetProperty("problem", out var problem))
                {
                    if (problem.TryGetProperty("rating", out var ratingProp))
                    {
                        rating = ratingProp.GetInt32();
                    }
                }

                if (rating <= 1200) easy++;
                else if (rating <= 1800) medium++;
                else hard++;
            }

            return new PlatformStatsDto
            {
                Platform = "Codeforces",
                Username = username,
                Easy = easy,
                Medium = medium,
                Hard = hard,
                TotalSolved = easy + medium + hard
            };
        }
    }
}