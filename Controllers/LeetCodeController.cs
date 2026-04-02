using Microsoft.AspNetCore.Mvc;
using CodeSolvedTracker.Services;

namespace CodeSolvedTracker.Controllers
{
    [ApiController]
    [Route("api/leetcode")]
    public class LeetCodeController : Controller
    {
        private readonly LeetCodeService _service;

        public LeetCodeController(LeetCodeService service)
        {
            _service = service;
        }

        [HttpGet("{username}")]
        public async Task<IActionResult> Get(string username)
        {
            var solved = await _service.GetSolvedCount(username);
            return Ok(new { solved });
        }
    }
}