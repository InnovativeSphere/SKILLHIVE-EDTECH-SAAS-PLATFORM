using Microsoft.EntityFrameworkCore;
using SkillHive.Models;

namespace SkillHive.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users => Set<User>();
        public DbSet<Academy> Academies => Set<Academy>();
        public DbSet<VerificationToken> VerificationTokens => Set<VerificationToken>();
        public DbSet<Notification> Notifications => Set<Notification>();

        public DbSet<Category> Categories => Set<Category>();
        public DbSet<Profession> Professions => Set<Profession>();

        public DbSet<Course> Courses => Set<Course>();
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Table names
            modelBuilder.Entity<User>().ToTable("USERS");
            modelBuilder.Entity<Academy>().ToTable("ACADEMIES");
            modelBuilder.Entity<VerificationToken>().ToTable("VERIFICATION_TOKENS");
            modelBuilder.Entity<Notification>().ToTable("NOTIFICATIONS");

            // Unique constraints
            modelBuilder.Entity<User>().HasIndex(u => u.Email).IsUnique();
            modelBuilder.Entity<User>().HasIndex(u => u.Username).IsUnique();
            modelBuilder.Entity<Academy>().HasIndex(a => a.Slug).IsUnique();
            modelBuilder.Entity<VerificationToken>().HasIndex(v => v.Token).IsUnique();

            // Academy -> Owner (User) — no cascade delete
            modelBuilder.Entity<Academy>()
                .HasOne(a => a.Owner)
                .WithMany()
                .HasForeignKey(a => a.OwnerId)
                .OnDelete(DeleteBehavior.Restrict);

            // User -> Academy — no cascade delete
            modelBuilder.Entity<User>()
                .HasOne(u => u.Academy)
                .WithMany()
                .HasForeignKey(u => u.AcademyId)
                .OnDelete(DeleteBehavior.Restrict);

            // User -> InvitedBy (self-reference) — no cascade delete
            modelBuilder.Entity<User>()
                .HasOne(u => u.InvitedBy)
                .WithMany()
                .HasForeignKey(u => u.InvitedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            // VerificationToken -> User
            modelBuilder.Entity<VerificationToken>()
                .HasOne(v => v.User)
                .WithMany()
                .HasForeignKey(v => v.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Notification -> User
            modelBuilder.Entity<Notification>()
                .HasOne(n => n.User)
                .WithMany()
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Table names
            modelBuilder.Entity<Category>().ToTable("CATEGORIES");
            modelBuilder.Entity<Profession>().ToTable("PROFESSIONS");

            // Unique slugs per scope
            modelBuilder.Entity<Category>()
                .HasIndex(c => new { c.Slug, c.AcademyId })
                .IsUnique();

            modelBuilder.Entity<Profession>()
                .HasIndex(p => new { p.Slug, p.AcademyId })
                .IsUnique();

            // Category -> Academy (nullable)
            modelBuilder.Entity<Category>()
                .HasOne(c => c.Academy)
                .WithMany()
                .HasForeignKey(c => c.AcademyId)
                .OnDelete(DeleteBehavior.Restrict);

            // Profession -> Category
            modelBuilder.Entity<Profession>()
                .HasOne(p => p.Category)
                .WithMany()
                .HasForeignKey(p => p.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            // Profession -> Academy (nullable)
            modelBuilder.Entity<Profession>()
                .HasOne(p => p.Academy)
                .WithMany()
                .HasForeignKey(p => p.AcademyId)
                .OnDelete(DeleteBehavior.Restrict);

            // Table name
            modelBuilder.Entity<Course>().ToTable("COURSES");

            // Unique slug within an academy
            modelBuilder.Entity<Course>()
                .HasIndex(c => new { c.Slug, c.AcademyId })
                .IsUnique();

            // Course -> Academy
            modelBuilder.Entity<Course>()
                .HasOne(c => c.Academy)
                .WithMany()
                .HasForeignKey(c => c.AcademyId)
                .OnDelete(DeleteBehavior.Restrict);

            // Course -> Instructor (User)
            modelBuilder.Entity<Course>()
                .HasOne(c => c.Instructor)
                .WithMany()
                .HasForeignKey(c => c.InstructorId)
                .OnDelete(DeleteBehavior.Restrict);

            // Course -> CreatedBy (User)
            modelBuilder.Entity<Course>()
                .HasOne(c => c.CreatedBy)
                .WithMany()
                .HasForeignKey(c => c.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Course -> Profession
            modelBuilder.Entity<Course>()
                .HasOne(c => c.Profession)
                .WithMany()
                .HasForeignKey(c => c.ProfessionId)
                .OnDelete(DeleteBehavior.Restrict);
        }



    }

}