using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Roots_Wings.Models;

namespace Roots_Wings.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        // Tabelele aplicației
        public DbSet<TherapySession> TherapySessions { get; set; }
        public DbSet<JournalEntry> JournalEntries { get; set; }
        public DbSet<Tag> Tags { get; set; }
        public DbSet<Goal> Goals { get; set; }
        public DbSet<Milestone> Milestones { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Relația User - TherapySessions
            modelBuilder.Entity<TherapySession>()
                .HasOne(s => s.User)
                .WithMany(u => u.TherapySessions)
                .HasForeignKey(s => s.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Relația User - JournalEntries
            modelBuilder.Entity<JournalEntry>()
                .HasOne(e => e.User)
                .WithMany(u => u.JournalEntries) 
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Relația Many-to-Many JournalEntry - Tag
            modelBuilder.Entity<JournalEntry>()
                .HasMany(e => e.Tags)
                .WithMany(t => t.JournalEntries)
                .UsingEntity(j => j.ToTable("JournalEntryTags"));

            // Constrângere unicitate pentru numele Tag-ului
            modelBuilder.Entity<Tag>()
                .HasIndex(t => t.Name)
                .IsUnique();

            // Relația User - Goals
            modelBuilder.Entity<Goal>()
                .HasOne(g => g.User)
                .WithMany(u => u.Goals)
                .HasForeignKey(g => g.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Relația Goal - Milestones
            // Deleting a goal deletes its milestones with it; milestones don't exist independently.
            modelBuilder.Entity<Milestone>()
                .HasOne(m => m.Goal)
                .WithMany(g => g.Milestones)
                .HasForeignKey(m => m.GoalId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}