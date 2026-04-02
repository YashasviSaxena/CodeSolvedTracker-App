using System;

namespace CodeSolvedTracker.Models
{
    public class UserPlatform
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Platform { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public int TotalSolved { get; set; }
        public int EasySolved { get; set; }
        public int MediumSolved { get; set; }
        public int HardSolved { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime LastSynced { get; set; }

        public virtual User? User { get; set; }
    }
}