namespace CodeSolvedTracker.Models
{
    public class PlatformStatsDto
    {
        public string Platform { get; set; }
        public string Username { get; set; }
        public int TotalSolved { get; set; }
        public int Easy { get; set; }
        public int Medium { get; set; }
        public int Hard { get; set; }
    }
}