using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CodeSolvedTracker.Data;
using CodeSolvedTracker.Models;
using System.Security.Claims;
using System.Text.Json;
using System.Text;
using System.Text.RegularExpressions;

namespace CodeSolvedTracker.Controllers
{
    [Authorize]
    public class PlatformsController : Controller
    {
        private readonly AppDbContext _context;
        private readonly HttpClient _httpClient;

        public PlatformsController(AppDbContext context, IHttpClientFactory httpClientFactory)
        {
            _context = context;
            _httpClient = httpClientFactory.CreateClient();
            _httpClient.Timeout = TimeSpan.FromSeconds(60);
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
        }

        public async Task<IActionResult> Index()
        {
            var userEmail = User.FindFirst(ClaimTypes.Name)?.Value;
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == userEmail);

            if (user == null)
                return RedirectToAction("Login", "Account");

            var platforms = await _context.UserPlatforms
                .Where(p => p.UserId == user.Id)
                .ToListAsync();

            return View(platforms);
        }

        [HttpPost]
        public async Task<IActionResult> SyncLeetCode([FromBody] SyncRequest request)
        {
            try
            {
                var username = request?.Username?.Trim();

                if (string.IsNullOrWhiteSpace(username))
                {
                    return Json(new { success = false, message = "Please enter a LeetCode username" });
                }

                var userEmail = User.FindFirst(ClaimTypes.Name)?.Value;
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == userEmail);

                if (user == null)
                    return Json(new { success = false, message = "User not found" });

                // Alternative LeetCode API endpoint
                var leetCodeApiUrl = $"https://leetcode-stats-api.herokuapp.com/{username}";
                var response = await _httpClient.GetAsync(leetCodeApiUrl);

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var data = JsonSerializer.Deserialize<JsonElement>(json);

                    if (data.TryGetProperty("status", out var status) && status.GetString() == "success")
                    {
                        var totalSolved = data.GetProperty("totalSolved").GetInt32();
                        var easy = data.GetProperty("easySolved").GetInt32();
                        var medium = data.GetProperty("mediumSolved").GetInt32();
                        var hard = data.GetProperty("hardSolved").GetInt32();
                        var leetcodeUsername = username;

                        // Save or update platform
                        var platform = await _context.UserPlatforms
                            .FirstOrDefaultAsync(p => p.UserId == user.Id && p.Platform == "LeetCode");

                        if (platform == null)
                        {
                            platform = new UserPlatform
                            {
                                UserId = user.Id,
                                Platform = "LeetCode",
                                Username = leetcodeUsername,
                                TotalSolved = totalSolved,
                                EasySolved = easy,
                                MediumSolved = medium,
                                HardSolved = hard,
                                CreatedAt = DateTime.UtcNow,
                                LastSynced = DateTime.UtcNow
                            };
                            _context.UserPlatforms.Add(platform);
                        }
                        else
                        {
                            platform.Username = leetcodeUsername;
                            platform.TotalSolved = totalSolved;
                            platform.EasySolved = easy;
                            platform.MediumSolved = medium;
                            platform.HardSolved = hard;
                            platform.LastSynced = DateTime.UtcNow;
                        }

                        await _context.SaveChangesAsync();

                        // Update user stats
                        var allPlatforms = await _context.UserPlatforms.Where(p => p.UserId == user.Id).ToListAsync();
                        var grandTotal = allPlatforms.Sum(p => p.TotalSolved);

                        var userStats = await _context.Stats.FirstOrDefaultAsync(s => s.UserId == user.Id);
                        if (userStats == null)
                        {
                            userStats = new Stats { UserId = user.Id, TotalSolved = grandTotal, TotalProblems = grandTotal, LastUpdated = DateTime.UtcNow };
                            _context.Stats.Add(userStats);
                        }
                        else
                        {
                            userStats.TotalSolved = grandTotal;
                            userStats.TotalProblems = grandTotal;
                            userStats.LastUpdated = DateTime.UtcNow;
                        }
                        await _context.SaveChangesAsync();

                        return Json(new { success = true, totalSolved = totalSolved, easy = easy, medium = medium, hard = hard, message = $"Synced! Total: {totalSolved}" });
                    }
                    else
                    {
                        return Json(new { success = false, message = $"LeetCode username '{username}' not found." });
                    }
                }

                return Json(new { success = false, message = "Unable to connect to LeetCode. Please try again." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> SyncCodeforces([FromBody] SyncRequest request)
        {
            try
            {
                var username = request?.Username?.Trim();

                if (string.IsNullOrWhiteSpace(username))
                {
                    return Json(new { success = false, message = "Please enter a Codeforces username" });
                }

                var userEmail = User.FindFirst(ClaimTypes.Name)?.Value;
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == userEmail);

                if (user == null)
                    return Json(new { success = false, message = "User not found" });

                var response = await _httpClient.GetAsync($"https://codeforces.com/api/user.status?handle={username}&from=1&count=10000");
                var totalSolved = 0;
                var easy = 0;
                var medium = 0;
                var hard = 0;

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var data = JsonSerializer.Deserialize<JsonElement>(json);

                    if (data.GetProperty("status").GetString() == "OK")
                    {
                        var solved = new HashSet<string>();
                        var results = data.GetProperty("result");

                        foreach (var submission in results.EnumerateArray())
                        {
                            if (submission.GetProperty("verdict").GetString() == "OK")
                            {
                                var problem = submission.GetProperty("problem");
                                var contestId = problem.GetProperty("contestId").GetInt32();
                                var index = problem.GetProperty("index").GetString();
                                var problemId = $"{contestId}{index}";

                                var difficulty = "Medium";
                                if (problem.TryGetProperty("rating", out var ratingProp))
                                {
                                    var rating = ratingProp.GetInt32();
                                    if (rating <= 1200) difficulty = "Easy";
                                    else if (rating <= 2000) difficulty = "Medium";
                                    else difficulty = "Hard";
                                }

                                if (difficulty == "Easy") easy++;
                                else if (difficulty == "Medium") medium++;
                                else if (difficulty == "Hard") hard++;

                                if (!solved.Contains(problemId))
                                {
                                    solved.Add(problemId);
                                }
                            }
                        }
                        totalSolved = solved.Count;
                    }
                }

                var platform = await _context.UserPlatforms.FirstOrDefaultAsync(p => p.UserId == user.Id && p.Platform == "Codeforces");

                if (platform == null)
                {
                    platform = new UserPlatform
                    {
                        UserId = user.Id,
                        Platform = "Codeforces",
                        Username = username,
                        TotalSolved = totalSolved,
                        EasySolved = easy,
                        MediumSolved = medium,
                        HardSolved = hard,
                        CreatedAt = DateTime.UtcNow,
                        LastSynced = DateTime.UtcNow
                    };
                    _context.UserPlatforms.Add(platform);
                }
                else
                {
                    platform.Username = username;
                    platform.TotalSolved = totalSolved;
                    platform.EasySolved = easy;
                    platform.MediumSolved = medium;
                    platform.HardSolved = hard;
                    platform.LastSynced = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();

                var allPlatforms = await _context.UserPlatforms.Where(p => p.UserId == user.Id).ToListAsync();
                var grandTotal = allPlatforms.Sum(p => p.TotalSolved);

                var userStats = await _context.Stats.FirstOrDefaultAsync(s => s.UserId == user.Id);
                if (userStats == null)
                {
                    userStats = new Stats { UserId = user.Id, TotalSolved = grandTotal, TotalProblems = grandTotal, LastUpdated = DateTime.UtcNow };
                    _context.Stats.Add(userStats);
                }
                else
                {
                    userStats.TotalSolved = grandTotal;
                    userStats.TotalProblems = grandTotal;
                    userStats.LastUpdated = DateTime.UtcNow;
                }
                await _context.SaveChangesAsync();

                return Json(new { success = true, totalSolved = totalSolved, message = $"Codeforces synced! Total Solved: {totalSolved}" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> SyncCodeChef([FromBody] SyncRequest request)
        {
            try
            {
                var username = request?.Username?.Trim();

                if (string.IsNullOrWhiteSpace(username))
                {
                    return Json(new { success = false, message = "Please enter a CodeChef username" });
                }

                var userEmail = User.FindFirst(ClaimTypes.Name)?.Value;
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == userEmail);

                if (user == null)
                    return Json(new { success = false, message = "User not found" });

                var totalSolved = 0;

                try
                {
                    var response = await _httpClient.GetAsync($"https://www.codechef.com/users/{username}");
                    if (response.IsSuccessStatusCode)
                    {
                        var html = await response.Content.ReadAsStringAsync();
                        var match = Regex.Match(html, @"Problems Solved:?\s*(\d+)", RegexOptions.IgnoreCase);
                        if (match.Success) totalSolved = int.Parse(match.Groups[1].Value);
                    }
                }
                catch { }

                var platform = await _context.UserPlatforms.FirstOrDefaultAsync(p => p.UserId == user.Id && p.Platform == "CodeChef");

                if (platform == null)
                {
                    platform = new UserPlatform
                    {
                        UserId = user.Id,
                        Platform = "CodeChef",
                        Username = username,
                        TotalSolved = totalSolved,
                        CreatedAt = DateTime.UtcNow,
                        LastSynced = DateTime.UtcNow
                    };
                    _context.UserPlatforms.Add(platform);
                }
                else
                {
                    platform.Username = username;
                    platform.TotalSolved = totalSolved;
                    platform.LastSynced = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();

                var allPlatforms = await _context.UserPlatforms.Where(p => p.UserId == user.Id).ToListAsync();
                var grandTotal = allPlatforms.Sum(p => p.TotalSolved);

                var userStats = await _context.Stats.FirstOrDefaultAsync(s => s.UserId == user.Id);
                if (userStats == null)
                {
                    userStats = new Stats { UserId = user.Id, TotalSolved = grandTotal, TotalProblems = grandTotal, LastUpdated = DateTime.UtcNow };
                    _context.Stats.Add(userStats);
                }
                else
                {
                    userStats.TotalSolved = grandTotal;
                    userStats.TotalProblems = grandTotal;
                    userStats.LastUpdated = DateTime.UtcNow;
                }
                await _context.SaveChangesAsync();

                return Json(new { success = true, totalSolved = totalSolved, message = $"CodeChef synced! Total Solved: {totalSolved}" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> SyncHackerRank([FromBody] SyncRequest request)
        {
            try
            {
                var username = request?.Username?.Trim();

                if (string.IsNullOrWhiteSpace(username))
                {
                    return Json(new { success = false, message = "Please enter a HackerRank username" });
                }

                var userEmail = User.FindFirst(ClaimTypes.Name)?.Value;
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == userEmail);

                if (user == null)
                    return Json(new { success = false, message = "User not found" });

                var totalSolved = 0;

                try
                {
                    // Try HackerRank's unofficial API
                    var apiUrl = $"https://www.hackerrank.com/rest/hackers/{username}/recent_challenges?limit=100";
                    var response = await _httpClient.GetAsync(apiUrl);

                    if (response.IsSuccessStatusCode)
                    {
                        var json = await response.Content.ReadAsStringAsync();
                        var data = JsonSerializer.Deserialize<JsonElement>(json);

                        if (data.TryGetProperty("models", out var models))
                        {
                            var uniqueProblems = new HashSet<string>();
                            foreach (var challenge in models.EnumerateArray())
                            {
                                if (challenge.TryGetProperty("name", out var name))
                                {
                                    uniqueProblems.Add(name.GetString());
                                }
                            }
                            totalSolved = uniqueProblems.Count;
                        }
                    }

                    // Fallback to profile page
                    if (totalSolved == 0)
                    {
                        var profileUrl = $"https://www.hackerrank.com/profile/{username}";
                        var profileResponse = await _httpClient.GetAsync(profileUrl);

                        if (profileResponse.IsSuccessStatusCode)
                        {
                            var html = await profileResponse.Content.ReadAsStringAsync();
                            var solvedMatch = Regex.Match(html, @"(\d+)\s*Problems Solved", RegexOptions.IgnoreCase);
                            if (solvedMatch.Success)
                            {
                                totalSolved = int.Parse(solvedMatch.Groups[1].Value);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"HackerRank error: {ex.Message}");
                }

                var platform = await _context.UserPlatforms.FirstOrDefaultAsync(p => p.UserId == user.Id && p.Platform == "HackerRank");

                if (platform == null)
                {
                    platform = new UserPlatform
                    {
                        UserId = user.Id,
                        Platform = "HackerRank",
                        Username = username,
                        TotalSolved = totalSolved,
                        CreatedAt = DateTime.UtcNow,
                        LastSynced = DateTime.UtcNow
                    };
                    _context.UserPlatforms.Add(platform);
                }
                else
                {
                    platform.Username = username;
                    platform.TotalSolved = totalSolved;
                    platform.LastSynced = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();

                var allPlatforms = await _context.UserPlatforms.Where(p => p.UserId == user.Id).ToListAsync();
                var grandTotal = allPlatforms.Sum(p => p.TotalSolved);

                var userStats = await _context.Stats.FirstOrDefaultAsync(s => s.UserId == user.Id);
                if (userStats == null)
                {
                    userStats = new Stats { UserId = user.Id, TotalSolved = grandTotal, TotalProblems = grandTotal, LastUpdated = DateTime.UtcNow };
                    _context.Stats.Add(userStats);
                }
                else
                {
                    userStats.TotalSolved = grandTotal;
                    userStats.TotalProblems = grandTotal;
                    userStats.LastUpdated = DateTime.UtcNow;
                }
                await _context.SaveChangesAsync();

                if (totalSolved == 0)
                {
                    return Json(new { success = true, totalSolved = 0, message = $"HackerRank profile '{username}' added! Sync might take a few minutes to show solved problems." });
                }

                return Json(new { success = true, totalSolved = totalSolved, message = $"HackerRank synced! Total Solved: {totalSolved}" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        public async Task<IActionResult> GetPlatforms()
        {
            try
            {
                var userEmail = User.FindFirst(ClaimTypes.Name)?.Value;
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == userEmail);

                if (user == null)
                    return Json(new { success = false, message = "User not found" });

                var platforms = await _context.UserPlatforms
                    .Where(p => p.UserId == user.Id)
                    .Select(p => new { p.Platform, p.Username, p.TotalSolved, p.EasySolved, p.MediumSolved, p.HardSolved, p.CreatedAt, p.LastSynced })
                    .ToListAsync();

                var userStats = await _context.Stats.FirstOrDefaultAsync(s => s.UserId == user.Id);

                return Json(new { success = true, platforms = platforms, totalSolved = userStats?.TotalSolved ?? 0 });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }

    public class SyncRequest
    {
        public string? Username { get; set; }
    }
}