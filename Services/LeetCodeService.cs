using System.Text.Json;

namespace CodeSolvedTracker.Services
{
    public class LeetCodeService
    {
        private readonly HttpClient _http;

        public LeetCodeService(HttpClient http)
        {
            _http = http;
        }

        public async Task<int> GetSolvedCount(string username)
        {
            var url = $"https://leetcode-stats-api.herokuapp.com/{username}";
            var res = await _http.GetStringAsync(url);

            var data = JsonSerializer.Deserialize<JsonElement>(res);

            return data.GetProperty("totalSolved").GetInt32();
        }
    }
}