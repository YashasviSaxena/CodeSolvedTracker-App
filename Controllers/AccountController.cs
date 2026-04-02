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

        [HttpGet]
        public IActionResult Login()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Dashboard");
            }
            return View();
        }

        [HttpGet]
        public IActionResult Register()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Dashboard");
            }
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(string Email, string Password, string ConfirmPassword)
        {
            if (string.IsNullOrEmpty(Email))
            {
                TempData["ErrorMessage"] = "Email is required";
                return View();
            }

            if (string.IsNullOrEmpty(Password))
            {
                TempData["ErrorMessage"] = "Password is required";
                return View();
            }

            if (Password != ConfirmPassword)
            {
                TempData["ErrorMessage"] = "Passwords do not match";
                return View();
            }

            if (Password.Length < 6)
            {
                TempData["ErrorMessage"] = "Password must be at least 6 characters long";
                return View();
            }

            var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == Email);
            if (existingUser != null)
            {
                TempData["ErrorMessage"] = "User with this email already exists";
                return View();
            }

            try
            {
                var user = new User
                {
                    Email = Email,
                    UserName = Email.Split('@')[0],
                    Password = BCrypt.Net.BCrypt.HashPassword(Password),
                    AuthProvider = "Manual",
                    Role = "User",
                    CreatedAt = DateTime.UtcNow,
                    LastLoginAt = null
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                var stats = new Stats
                {
                    UserId = user.Id,
                    TotalSolved = 0,
                    TotalProblems = 0,
                    LastUpdated = DateTime.UtcNow
                };
                _context.Stats.Add(stats);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Registration successful! Please login.";
                return RedirectToAction("Login");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Registration failed: {ex.Message}";
                return View();
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string Email, string Password, bool RememberMe)
        {
            if (string.IsNullOrEmpty(Email))
            {
                TempData["ErrorMessage"] = "Email is required";
                return View();
            }

            if (string.IsNullOrEmpty(Password))
            {
                TempData["ErrorMessage"] = "Password is required";
                return View();
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == Email);

            if (user == null)
            {
                TempData["ErrorMessage"] = "No account found with this email. Please register first.";
                return View();
            }

            if (string.IsNullOrEmpty(user.Password) || user.AuthProvider != "Manual")
            {
                TempData["ErrorMessage"] = $"This account was created with {user.AuthProvider}. Please login using the {user.AuthProvider} button.";
                return View();
            }

            if (!BCrypt.Net.BCrypt.Verify(Password, user.Password))
            {
                TempData["ErrorMessage"] = "Invalid password. Please try again.";
                return View();
            }

            try
            {
                user.LastLoginAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, user.Email),
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim("Role", user.Role ?? "User"),
                    new Claim("AuthProvider", user.AuthProvider)
                };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = RememberMe,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7),
                    AllowRefresh = true
                };

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    authProperties);

                TempData["SuccessMessage"] = $"Welcome back, {user.UserName}!";
                return RedirectToAction("Dashboard");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Login failed: {ex.Message}";
                return View();
            }
        }

        [HttpGet]
        public IActionResult GoogleLogin()
        {
            var redirectUrl = Url.Action(nameof(ExternalLoginCallback), "Account", null, "https");
            var properties = new AuthenticationProperties
            {
                RedirectUri = redirectUrl,
                Items = { { "LoginProvider", "Google" } }
            };
            return Challenge(properties, "Google");
        }

        [HttpGet]
        public IActionResult GitHubLogin()
        {
            var redirectUrl = Url.Action(nameof(ExternalLoginCallback), "Account", null, "https");
            var properties = new AuthenticationProperties
            {
                RedirectUri = redirectUrl,
                Items = { { "LoginProvider", "GitHub" } }
            };
            return Challenge(properties, "GitHub");
        }

        [HttpGet]
        public async Task<IActionResult> ExternalLoginCallback(string returnUrl = null, string remoteError = null)
        {
            if (remoteError != null)
            {
                TempData["ErrorMessage"] = $"Error from external provider: {remoteError}";
                return RedirectToAction("Login");
            }

            var result = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            if (!result.Succeeded)
            {
                TempData["ErrorMessage"] = "External login failed. Please try again.";
                return RedirectToAction("Login");
            }

            var email = result.Principal?.FindFirst(ClaimTypes.Email)?.Value;
            var name = result.Principal?.FindFirst(ClaimTypes.Name)?.Value;

            if (string.IsNullOrEmpty(email))
            {
                email = result.Principal?.FindFirst("urn:github:email")?.Value;
            }

            var provider = result.Properties?.Items.ContainsKey("LoginProvider") == true
                ? result.Properties.Items["LoginProvider"]
                : "External";

            if (string.IsNullOrEmpty(email))
            {
                TempData["ErrorMessage"] = "Could not retrieve email from external provider.";
                return RedirectToAction("Login");
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

            if (user == null)
            {
                user = new User
                {
                    Email = email,
                    UserName = name ?? email.Split('@')[0],
                    Password = null,
                    AuthProvider = provider,
                    Role = "User",
                    CreatedAt = DateTime.UtcNow,
                    LastLoginAt = DateTime.UtcNow
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                var stats = new Stats
                {
                    UserId = user.Id,
                    TotalSolved = 0,
                    TotalProblems = 0,
                    LastUpdated = DateTime.UtcNow
                };
                _context.Stats.Add(stats);
                await _context.SaveChangesAsync();
            }
            else
            {
                user.LastLoginAt = DateTime.UtcNow;
                if (user.AuthProvider != provider)
                {
                    user.AuthProvider = provider;
                }
                await _context.SaveChangesAsync();
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Email),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim("Role", user.Role ?? "User"),
                new Claim("AuthProvider", user.AuthProvider)
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity));

            TempData["SuccessMessage"] = $"Welcome {user.UserName}! You have successfully logged in with {provider}.";

            return RedirectToAction("Dashboard");
        }

        [Authorize]
        public async Task<IActionResult> Dashboard()
        {
            var userEmail = User.FindFirst(ClaimTypes.Name)?.Value ??
                            User.FindFirst(ClaimTypes.Email)?.Value;

            if (string.IsNullOrEmpty(userEmail))
            {
                return RedirectToAction("Login");
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == userEmail);

            if (user == null)
            {
                return RedirectToAction("Login");
            }

            var userPlatforms = await _context.UserPlatforms
                .Where(up => up.UserId == user.Id)
                .ToListAsync();

            var recentProblems = await _context.Problems
                .Where(p => p.UserId == user.Id && p.IsSolved)
                .OrderByDescending(p => p.SolvedAt ?? p.CreatedAt)
                .Take(10)
                .ToListAsync();

            var stats = await _context.Stats
                .FirstOrDefaultAsync(s => s.UserId == user.Id);

            // Calculate difficulty counts from all platforms
            var easyCount = userPlatforms.Sum(p => p.EasySolved);
            var mediumCount = userPlatforms.Sum(p => p.MediumSolved);
            var hardCount = userPlatforms.Sum(p => p.HardSolved);

            // Calculate total solved from platforms
            var totalSolved = userPlatforms.Sum(p => p.TotalSolved);

            // Get weekly progress (last 7 days)
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

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            HttpContext.Session.Clear();
            TempData["SuccessMessage"] = "You have been logged out successfully.";
            return RedirectToAction("Login");
        }

        [Authorize]
        public IActionResult Profile()
        {
            return View();
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            try
            {
                if (request == null || string.IsNullOrEmpty(request.CurrentPassword) || string.IsNullOrEmpty(request.NewPassword))
                {
                    return Json(new { success = false, message = "Current password and new password are required" });
                }

                var userEmail = User.FindFirst(ClaimTypes.Name)?.Value ??
                                User.FindFirst(ClaimTypes.Email)?.Value;

                if (string.IsNullOrEmpty(userEmail))
                {
                    return Json(new { success = false, message = "User not authenticated" });
                }

                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == userEmail);

                if (user == null)
                {
                    return Json(new { success = false, message = "User not found" });
                }

                if (user.AuthProvider != "Manual")
                {
                    return Json(new { success = false, message = $"Password cannot be changed for {user.AuthProvider} accounts." });
                }

                if (string.IsNullOrEmpty(user.Password) || !BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.Password))
                {
                    return Json(new { success = false, message = "Current password is incorrect" });
                }

                if (string.IsNullOrEmpty(request.NewPassword) || request.NewPassword.Length < 6)
                {
                    return Json(new { success = false, message = "New password must be at least 6 characters long" });
                }

                user.Password = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Password changed successfully" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAccount()
        {
            try
            {
                var userEmail = User.FindFirst(ClaimTypes.Name)?.Value ??
                                User.FindFirst(ClaimTypes.Email)?.Value;

                if (string.IsNullOrEmpty(userEmail))
                {
                    return Json(new { success = false, message = "User not authenticated" });
                }

                var user = await _context.Users
                    .Include(u => u.UserPlatforms)
                    .Include(u => u.Problems)
                    .FirstOrDefaultAsync(u => u.Email == userEmail);

                if (user == null)
                {
                    return Json(new { success = false, message = "User not found" });
                }

                if (user.UserPlatforms != null && user.UserPlatforms.Any())
                {
                    _context.UserPlatforms.RemoveRange(user.UserPlatforms);
                }

                if (user.Problems != null && user.Problems.Any())
                {
                    _context.Problems.RemoveRange(user.Problems);
                }

                var stats = await _context.Stats.FirstOrDefaultAsync(s => s.UserId == user.Id);
                if (stats != null)
                {
                    _context.Stats.Remove(stats);
                }

                _context.Users.Remove(user);
                await _context.SaveChangesAsync();

                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                HttpContext.Session.Clear();

                return Json(new { success = true, message = "Account deleted successfully" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }
    }

    public class ChangePasswordRequest
    {
        public string? CurrentPassword { get; set; }
        public string? NewPassword { get; set; }
    }
}