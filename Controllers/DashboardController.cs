using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CodeSolvedTracker.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace CodeSolvedTracker.Controllers
{
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [ApiController]
    [Route("api/dashboard")]
    public class DashboardController : Controller
    {
        private readonly AppDbContext _context;

        public DashboardController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult GetStats()
        {
            var email = User.Identity?.Name;
            var user = _context.Users.FirstOrDefault(u => u.Email == email);

            var problems = _context.Problems.Where(p => p.UserId == user.Id);

            return Ok(new
            {
                total = problems.Count(),
                solved = problems.Count(p => p.IsSolved),
                easy = problems.Count(p => p.Difficulty == "Easy"),
                medium = problems.Count(p => p.Difficulty == "Medium"),
                hard = problems.Count(p => p.Difficulty == "Hard")
            });
        }
    }
}