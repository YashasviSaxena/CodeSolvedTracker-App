using System;
using System.Collections.Generic;

namespace CodeSolvedTracker.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Email { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; } // Allow null for OAuth users
        public string AuthProvider { get; set; } // Manual, Google, GitHub
        public string Role { get; set; } = "User"; // Admin, User
        public DateTime CreatedAt { get; set; }
        public DateTime? LastLoginAt { get; set; }

        // Navigation properties
        public virtual ICollection<UserPlatform> UserPlatforms { get; set; } = new List<UserPlatform>();
        public virtual ICollection<Problem> Problems { get; set; } = new List<Problem>();
        public virtual Stats Stats { get; set; }
    }
}