using CodeSolvedTracker.Models;

namespace CodeSolvedTracker.ViewModels
{
    public class DashboardViewModel
    {
        public string UserEmail { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public Stats? Stats { get; set; }
        public List<UserPlatform> UserPlatforms { get; set; } = new List<UserPlatform>();
        public List<Problem> RecentProblems { get; set; } = new List<Problem>();
        public int TotalSolved { get; set; }
        public int TotalProblems { get; set; }
        public int EasyCount { get; set; }
        public int MediumCount { get; set; }
        public int HardCount { get; set; }
        public List<string> ProgressDates { get; set; } = new List<string>();
        public List<int> ProgressCounts { get; set; } = new List<int>();
    }
}