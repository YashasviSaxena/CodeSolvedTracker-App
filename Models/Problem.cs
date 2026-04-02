using System;

namespace CodeSolvedTracker.Models
{
    public class Problem
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Platform { get; set; } = string.Empty;
        public string Difficulty { get; set; } = string.Empty;
        public bool IsSolved { get; set; }
        public int UserId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? SolvedAt { get; set; }

        public virtual User? User { get; set; }
    }
}