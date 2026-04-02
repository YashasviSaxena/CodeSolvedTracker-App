using Microsoft.AspNetCore.Mvc;
using CodeSolvedTracker.Data;
using CodeSolvedTracker.Models;
using System.Security.Claims;

namespace CodeSolvedTracker.Controllers
{
    public class ProblemController : Controller
    {
        private readonly AppDbContext _context;

        public ProblemController(AppDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var email = User.Identity.Name;
            var user = _context.Users.FirstOrDefault(u => u.Email == email);

            var problems = _context.Problems.Where(p => p.UserId == user.Id).ToList();

            return View(problems);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Create(Problem model)
        {
            var email = User.Identity.Name;
            var user = _context.Users.FirstOrDefault(u => u.Email == email);

            model.UserId = user.Id;

            _context.Problems.Add(model);
            _context.SaveChanges();

            return RedirectToAction("Index");
        }

        public IActionResult Toggle(int id)
        {
            var problem = _context.Problems.Find(id);
            problem.IsSolved = !problem.IsSolved;

            _context.SaveChanges();

            return RedirectToAction("Index");
        }
    }
}