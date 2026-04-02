using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CodeSolvedTracker.Data;
using CodeSolvedTracker.Services;
using System.Security.Claims;

namespace CodeSolvedTracker.Controllers
{
    [Authorize]
    public class AIController : Controller
    {
        private readonly AppDbContext _context;

        public AIController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Insights()
        {
            var userEmail = User.FindFirst(ClaimTypes.Name)?.Value;
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == userEmail);
            
            if (user == null)
                return RedirectToAction("Login", "Account");

            // First fetch the problems, then process them in memory
            var problems = await _context.Problems
                .Where(p => p.UserId == user.Id)
                .ToListAsync();

            // Process in memory (not in the database query)
            var userProblems = problems.Select(p => new ProblemData
            {
                Platform = p.Platform,
                Difficulty = p.Difficulty,
                Topic = GetProblemTopic(p.Title),  // Now this works because we're in memory
                IsSolved = p.IsSolved,
                TimeSpentMinutes = 15
            }).ToList();

            var aiService = new AIService();
            var weakTopics = aiService.GetWeakTopics(userProblems);
            var recommendedDifficulty = aiService.GetRecommendedDifficulty(userProblems);
            
            var recentProblems = problems
                .Where(p => p.IsSolved)
                .OrderByDescending(p => p.SolvedAt)
                .Take(10)
                .ToList();

            ViewBag.WeakTopics = weakTopics;
            ViewBag.RecommendedDifficulty = recommendedDifficulty.RecommendedDifficulty;
            ViewBag.Confidence = recommendedDifficulty.Confidence;
            ViewBag.RecentProblems = recentProblems;
            ViewBag.TotalSolved = userProblems.Count(p => p.IsSolved);
            
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetRecommendations()
        {
            var userEmail = User.FindFirst(ClaimTypes.Name)?.Value;
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == userEmail);
            
            if (user == null)
                return Json(new { success = false, message = "User not found" });

            // First fetch the problems, then process them in memory
            var problems = await _context.Problems
                .Where(p => p.UserId == user.Id)
                .ToListAsync();

            // Process in memory (not in the database query)
            var userProblems = problems.Select(p => new ProblemData
            {
                Platform = p.Platform,
                Difficulty = p.Difficulty,
                Topic = GetProblemTopic(p.Title),  // Now this works because we're in memory
                IsSolved = p.IsSolved,
                TimeSpentMinutes = 15
            }).ToList();

            var aiService = new AIService();
            var weakTopics = aiService.GetWeakTopics(userProblems);
            var recommendedDifficulty = aiService.GetRecommendedDifficulty(userProblems);
            
            return Json(new
            {
                success = true,
                weakTopics = weakTopics,
                recommendedDifficulty = recommendedDifficulty.RecommendedDifficulty,
                confidence = recommendedDifficulty.Confidence,
                totalSolved = userProblems.Count(p => p.IsSolved),
                suggestedProblems = GetSuggestedProblems(weakTopics, recommendedDifficulty.RecommendedDifficulty)
            });
        }

        // Made static to avoid EF Core translation issues
        private static string GetProblemTopic(string title)
        {
            if (string.IsNullOrEmpty(title)) return "Algorithms";
            
            var titleLower = title.ToLower();
            if (titleLower.Contains("array") || titleLower.Contains("two sum")) return "Arrays";
            if (titleLower.Contains("string")) return "Strings";
            if (titleLower.Contains("tree") || titleLower.Contains("binary")) return "Trees";
            if (titleLower.Contains("graph")) return "Graphs";
            if (titleLower.Contains("dp") || titleLower.Contains("dynamic")) return "Dynamic Programming";
            if (titleLower.Contains("sort")) return "Sorting";
            if (titleLower.Contains("search") || titleLower.Contains("binary search")) return "Searching";
            return "Algorithms";
        }

        private static List<string> GetSuggestedProblems(List<string> weakTopics, string difficulty)
        {
            var suggestions = new List<string>();
            var topic = weakTopics.FirstOrDefault() ?? "Arrays";
            
            suggestions.Add($"Practice {difficulty} level problems on {topic}");
            suggestions.Add($"Try solving 3-5 problems daily on {topic}");
            suggestions.Add($"Focus on {difficulty} difficulty to improve your skills");
            suggestions.Add($"Review your past mistakes on {topic}");
            
            return suggestions;
        }
    }
}