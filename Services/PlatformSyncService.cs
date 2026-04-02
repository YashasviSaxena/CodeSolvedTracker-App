using CodeSolvedTracker.Data;
using CodeSolvedTracker.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace CodeSolvedTracker.Services
{
    public class PlatformSyncService
    {
        private readonly HttpClient _httpClient;
        private readonly AppDbContext _context;

        public PlatformSyncService(HttpClient httpClient, AppDbContext context)
        {
            _httpClient = httpClient;
            _context = context;
        }

        public async Task<PlatformStats> SyncLeetCode(string username)
        {
            try
            {
                // LeetCode GraphQL API
                var query = @"{
                    matchedUser(username: """ + username + @""") {
                        username
                        submitStats {
                            acSubmissionNum {
                                difficulty
                                count
                            }
                        }
                    }
                }";

                var request = new
                {
                    query = query,
                    variables = new { }
                };

                var content = new StringContent(JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync("https://leetcode.com/graphql", content);

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var data = JsonSerializer.Deserialize<JsonElement>(json);

                    var stats = new PlatformStats
                    {
                        Platform = "LeetCode",
                        Username = username,
                        TotalSolved = 0,
                        Easy = 0,
                        Medium = 0,
                        Hard = 0,
                        LastSynced = DateTime.UtcNow
                    };

                    if (data.TryGetProperty("data", out var dataObj) &&
                        dataObj.TryGetProperty("matchedUser", out var userObj) &&
                        userObj.TryGetProperty("submitStats", out var statsObj))
                    {
                        var submissions = statsObj.GetProperty("acSubmissionNum");
                        foreach (var sub in submissions.EnumerateArray())
                        {
                            var difficulty = sub.GetProperty("difficulty").GetString();
                            var count = sub.GetProperty("count").GetInt32();

                            stats.TotalSolved += count;

                            switch (difficulty)
                            {
                                case "Easy": stats.Easy = count; break;
                                case "Medium": stats.Medium = count; break;
                                case "Hard": stats.Hard = count; break;
                            }
                        }
                    }

                    return stats;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error syncing LeetCode: {ex.Message}");
            }

            return null;
        }

        public async Task<PlatformStats> SyncCodeforces(string username)
        {
            try
            {
                var response = await _httpClient.GetAsync($"https://codeforces.com/api/user.status?handle={username}&from=1&count=10000");

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var data = JsonSerializer.Deserialize<JsonElement>(json);

                    if (data.GetProperty("status").GetString() == "OK")
                    {
                        var problems = new Dictionary<string, bool>();
                        var results = data.GetProperty("result");

                        foreach (var submission in results.EnumerateArray())
                        {
                            var problem = submission.GetProperty("problem");
                            var problemId = $"{problem.GetProperty("contestId").GetInt32()}_{problem.GetProperty("index").GetString()}";
                            var verdict = submission.GetProperty("verdict").GetString();

                            if (verdict == "OK" && !problems.ContainsKey(problemId))
                            {
                                problems[problemId] = true;
                            }
                        }

                        return new PlatformStats
                        {
                            Platform = "Codeforces",
                            Username = username,
                            TotalSolved = problems.Count,
                            Easy = 0, // Codeforces doesn't have difficulty categories
                            Medium = 0,
                            Hard = 0,
                            LastSynced = DateTime.UtcNow
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error syncing Codeforces: {ex.Message}");
            }

            return null;
        }
    }

    public class PlatformStats
    {
        public string Platform { get; set; }
        public string Username { get; set; }
        public int TotalSolved { get; set; }
        public int Easy { get; set; }
        public int Medium { get; set; }
        public int Hard { get; set; }
        public DateTime LastSynced { get; set; }
    }
}