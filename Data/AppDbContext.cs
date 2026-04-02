using Microsoft.EntityFrameworkCore;
using CodeSolvedTracker.Models;

namespace CodeSolvedTracker.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<UserPlatform> UserPlatforms { get; set; }
        public DbSet<Problem> Problems { get; set; }
        public DbSet<Stats> Stats { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // User configuration
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
                entity.HasIndex(e => e.Email).IsUnique();
                entity.Property(e => e.UserName).HasMaxLength(100);
                entity.Property(e => e.Password).HasMaxLength(255).IsRequired(false); // Allow null
                entity.Property(e => e.AuthProvider).HasMaxLength(50).HasDefaultValue("Manual");
                entity.Property(e => e.Role).HasMaxLength(20).HasDefaultValue("User");
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(e => e.LastLoginAt).IsRequired(false);
            });

            // UserPlatform configuration
            modelBuilder.Entity<UserPlatform>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Platform).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Username).IsRequired().HasMaxLength(100);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.HasOne(up => up.User)
                    .WithMany(u => u.UserPlatforms)
                    .HasForeignKey(up => up.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Problem configuration
            modelBuilder.Entity<Problem>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Title).IsRequired().HasMaxLength(500);
                entity.Property(e => e.Platform).HasMaxLength(50);
                entity.Property(e => e.Difficulty).HasMaxLength(20);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.HasOne(p => p.User)
                    .WithMany(u => u.Problems)
                    .HasForeignKey(p => p.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Stats configuration
            modelBuilder.Entity<Stats>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.TotalSolved).HasDefaultValue(0);
                entity.Property(e => e.TotalProblems).HasDefaultValue(0);
                entity.Property(e => e.LastUpdated).HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.HasOne(s => s.User)
                    .WithOne(u => u.Stats)
                    .HasForeignKey<Stats>(s => s.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}