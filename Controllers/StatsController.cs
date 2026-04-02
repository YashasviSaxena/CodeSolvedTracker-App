using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using CodeSolvedTracker.Data;
using CodeSolvedTracker.Models;
using System.Security.Claims;

namespace CodeSolvedTracker.Controllers
{
    [Authorize]
    public class StatsController : Controller
    {
        private readonly AppDbContext _context;

        public StatsController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var userEmail = User.FindFirst(ClaimTypes.Name)?.Value ??
                            User.FindFirst(ClaimTypes.Email)?.Value;

            if (string.IsNullOrEmpty(userEmail))
            {
                return RedirectToAction("Login", "Account");
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == userEmail);

            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var stats = await _context.Stats
                .FirstOrDefaultAsync(s => s.UserId == user.Id);

            var problemsByDifficulty = await _context.Problems
                .Where(p => p.UserId == user.Id)
                .GroupBy(p => p.Difficulty)
                .Select(g => new
                {
                    Difficulty = g.Key,
                    Total = g.Count(),
                    Solved = g.Count(p => p.IsSolved)
                })
                .ToListAsync();

            var viewModel = new
            {
                Stats = stats,
                ProblemsByDifficulty = problemsByDifficulty,
                TotalSolved = stats?.TotalSolved ?? 0,
                TotalProblems = stats?.TotalProblems ?? 0,
                CompletionRate = stats != null && stats.TotalProblems > 0
                    ? (double)stats.TotalSolved / stats.TotalProblems * 100
                    : 0
            };

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateStats(int userId, int solved, int total)
        {
            var stats = await _context.Stats
                .FirstOrDefaultAsync(s => s.UserId == userId);

            if (stats == null)
            {
                stats = new Stats
                {
                    UserId = userId,
                    TotalSolved = solved,
                    TotalProblems = total,
                    LastUpdated = DateTime.UtcNow
                };
                _context.Stats.Add(stats);
            }
            else
            {
                stats.TotalSolved = solved;
                stats.TotalProblems = total;
                stats.LastUpdated = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            return Ok(new { success = true });
        }

        public async Task<IActionResult> PlatformStats(string platform)
        {
            var userEmail = User.FindFirst(ClaimTypes.Name)?.Value;
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == userEmail);

            if (user == null)
            {
                return Unauthorized();
            }

            var platformProblems = await _context.Problems
                .Where(p => p.UserId == user.Id && p.Platform == platform)
                .ToListAsync();

            var stats = new
            {
                Platform = platform,
                Total = platformProblems.Count,
                Solved = platformProblems.Count(p => p.IsSolved),
                ByDifficulty = platformProblems
                    .GroupBy(p => p.Difficulty)
                    .Select(g => new
                    {
                        Difficulty = g.Key,
                        Total = g.Count(),
                        Solved = g.Count(p => p.IsSolved)
                    })
            };

            return Ok(stats);
        }
    }
}