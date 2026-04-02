using System;

namespace CodeSolvedTracker.Models
{
    public class Stats
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int TotalSolved { get; set; }
        public int TotalProblems { get; set; }
        public DateTime LastUpdated { get; set; }

        // Navigation property
        public virtual User User { get; set; }
    }
}