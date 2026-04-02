using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using CodeSolvedTracker.Data;
using CodeSolvedTracker.Models;
using CodeSolvedTracker.ViewModels;
using System.Security.Claims;

namespace CodeSolvedTracker.Controllers
{
    public class AccountController : Controller
    {
        private readonly AppDbContext _context;

        public AccountController(AppDbContext context)
        {
            _context = context;
        }

        // ================= LOGIN =================

        [HttpGet]
        public IActionResult Login()
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction("Dashboard");

            return View();
        }

        // ================= REGISTER =================

        [HttpGet]
        public IActionResult Register()
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction("Dashboard");

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(string Email, string Password, string ConfirmPassword)
        {
            if (string.IsNullOrEmpty(Email) || string.IsNullOrEmpty(Password))
            {
                TempData["ErrorMessage"] = "Email and Password required";
                return View();
            }

            if (Password != ConfirmPassword)
            {
                TempData["ErrorMessage"] = "Passwords do not match";
                return View();
            }

            var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == Email);
            if (existingUser != null)
            {
                TempData["ErrorMessage"] = "User already exists";
                return View();
            }

            var user = new User
            {
                Email = Email,
                UserName = Email.Split('@')[0],
                Password = BCrypt.Net.BCrypt.HashPassword(Password),
                AuthProvider = "Manual",
                Role = "User",
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            _context.Stats.Add(new Stats
            {
                UserId = user.Id,
                TotalSolved = 0,
                TotalProblems = 0,
                LastUpdated = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Registration successful!";
            return RedirectToAction("Login");
        }

        // ================= LOGIN POST =================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string Email, string Password, bool RememberMe)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == Email);

            if (user == null || !BCrypt.Net.BCrypt.Verify(Password, user.Password))
            {
                TempData["ErrorMessage"] = "Invalid credentials";
                return View();
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Email),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim("Role", user.Role ?? "User")
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(identity),
                new AuthenticationProperties { IsPersistent = RememberMe });

            return RedirectToAction("Dashboard");
        }

        // ================= GOOGLE LOGIN =================

        [HttpGet]
        public IActionResult GoogleLogin()
        {
            var properties = new AuthenticationProperties
            {
                RedirectUri = "/Account/ExternalLoginCallback"
            };

            return Challenge(properties, "Google");
        }

        // ================= GITHUB LOGIN =================

        [HttpGet]
        public IActionResult GitHubLogin()
        {
            var properties = new AuthenticationProperties
            {
                RedirectUri = "/Account/ExternalLoginCallback"
            };

            return Challenge(properties, "GitHub");
        }

        // ================= EXTERNAL CALLBACK =================

        [HttpGet]
        public async Task<IActionResult> ExternalLoginCallback(string returnUrl = null, string remoteError = null)
        {
            if (remoteError != null)
            {
                TempData["ErrorMessage"] = remoteError;
                return RedirectToAction("Login");
            }

            // Try Google first
            var result = await HttpContext.AuthenticateAsync("Google");

            // If not Google, try GitHub
            if (!result.Succeeded)
                result = await HttpContext.AuthenticateAsync("GitHub");

            if (!result.Succeeded)
            {
                TempData["ErrorMessage"] = "External login failed";
                return RedirectToAction("Login");
            }

            var email = result.Principal?.FindFirst(ClaimTypes.Email)?.Value;
            var name = result.Principal?.FindFirst(ClaimTypes.Name)?.Value;

            if (string.IsNullOrEmpty(email))
            {
                TempData["ErrorMessage"] = "Email not received";
                return RedirectToAction("Login");
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

            if (user == null)
            {
                user = new User
                {
                    Email = email,
                    UserName = name ?? email,
                    AuthProvider = "External",
                    CreatedAt = DateTime.UtcNow,
                    LastLoginAt = DateTime.UtcNow,
                    Role = "User"
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                // Create stats for new user
                _context.Stats.Add(new Stats
                {
                    UserId = user.Id,
                    TotalSolved = 0,
                    TotalProblems = 0,
                    LastUpdated = DateTime.UtcNow
                });
                await _context.SaveChangesAsync();
            }
            else
            {
                user.LastLoginAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Email),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim("Role", user.Role ?? "User")
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(identity));

            return RedirectToAction("Dashboard");
        }

        // ================= DASHBOARD =================

        [Authorize]
        public async Task<IActionResult> Dashboard()
        {
            var email = User.FindFirst(ClaimTypes.Email)?.Value ??
                        User.FindFirst(ClaimTypes.Name)?.Value;

            if (string.IsNullOrEmpty(email))
            {
                return RedirectToAction("Login");
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

            if (user == null)
            {
                return RedirectToAction("Login");
            }

            // Get connected platforms with their solved counts
            var userPlatforms = await _context.UserPlatforms
                .Where(up => up.UserId == user.Id)
                .ToListAsync();

            // Calculate total solved from all platforms
            var totalSolved = userPlatforms.Sum(p => p.TotalSolved);

            // Calculate difficulty breakdown from platforms
            var easyCount = userPlatforms.Sum(p => p.EasySolved);
            var mediumCount = userPlatforms.Sum(p => p.MediumSolved);
            var hardCount = userPlatforms.Sum(p => p.HardSolved);

            // Get recent solved problems
            var recentProblems = await _context.Problems
                .Where(p => p.UserId == user.Id && p.IsSolved)
                .OrderByDescending(p => p.SolvedAt ?? p.CreatedAt)
                .Take(10)
                .ToListAsync();

            // Update or create Stats record
            var stats = await _context.Stats.FirstOrDefaultAsync(s => s.UserId == user.Id);
            if (stats == null)
            {
                stats = new Stats
                {
                    UserId = user.Id,
                    TotalSolved = totalSolved,
                    TotalProblems = totalSolved,
                    LastUpdated = DateTime.UtcNow
                };
                _context.Stats.Add(stats);
            }
            else
            {
                stats.TotalSolved = totalSolved;
                stats.TotalProblems = totalSolved;
                stats.LastUpdated = DateTime.UtcNow;
            }
            await _context.SaveChangesAsync();

            // Weekly progress (last 7 days)
            var progressDates = new List<string>();
            var progressCounts = new List<int>();

            for (int i = 6; i >= 0; i--)
            {
                var date = DateTime.UtcNow.AddDays(-i).Date;
                var count = await _context.Problems
                    .CountAsync(p => p.UserId == user.Id &&
                                    p.IsSolved &&
                                    p.SolvedAt.HasValue &&
                                    p.SolvedAt.Value.Date == date);
                progressDates.Add(date.ToString("MMM dd"));
                progressCounts.Add(count);
            }

            var viewModel = new DashboardViewModel
            {
                UserEmail = user.Email,
                UserName = user.UserName,
                Stats = stats,
                UserPlatforms = userPlatforms,
                RecentProblems = recentProblems,
                TotalSolved = totalSolved,
                TotalProblems = totalSolved,
                EasyCount = easyCount,
                MediumCount = mediumCount,
                HardCount = hardCount,
                ProgressDates = progressDates,
                ProgressCounts = progressCounts
            };

            return View(viewModel);
        }

        // ================= LOGOUT =================

        [Authorize]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync();
            TempData["SuccessMessage"] = "Logged out successfully";
            return RedirectToAction("Login");
        }
    }
}