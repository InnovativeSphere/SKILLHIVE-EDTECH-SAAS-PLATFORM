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

        public DbSet<Lesson> Lessons => Set<Lesson>();

        public DbSet<Material> Materials => Set<Material>();

        public DbSet<Quiz> Quizzes => Set<Quiz>();
        public DbSet<Question> Questions => Set<Question>();
        public DbSet<Option> Options => Set<Option>();
        public DbSet<QuizAttempt> QuizAttempts => Set<QuizAttempt>();

        public DbSet<Enrollment> Enrollments => Set<Enrollment>();
        public DbSet<LessonProgress> LessonProgresses => Set<LessonProgress>();
        public DbSet<StudentAcademyFollow> StudentAcademyFollows => Set<StudentAcademyFollow>();

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

            modelBuilder.Entity<Lesson>().ToTable("LESSONS");

            // Unique order within a course (no two lessons same position)
            modelBuilder.Entity<Lesson>()
            .HasIndex(l => new { l.CourseId, l.Order })
            .IsUnique()
            .HasFilter("\"IsActive\" = true");

            // Lesson -> Course
            modelBuilder.Entity<Lesson>()
                .HasOne(l => l.Course)
                .WithMany()
                .HasForeignKey(l => l.CourseId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Material>().ToTable("MATERIALS");

            modelBuilder.Entity<Material>()
                .HasOne(m => m.Lesson)
                .WithMany()
                .HasForeignKey(m => m.LessonId)
                .OnDelete(DeleteBehavior.Cascade);

            // Table names
            modelBuilder.Entity<Quiz>().ToTable("QUIZZES");
            modelBuilder.Entity<Question>().ToTable("QUESTIONS");
            modelBuilder.Entity<Option>().ToTable("OPTIONS");
            modelBuilder.Entity<QuizAttempt>().ToTable("QUIZ_ATTEMPTS");

            // Quiz -> Course (nullable)
            modelBuilder.Entity<Quiz>()
                .HasOne(q => q.Course)
                .WithMany()
                .HasForeignKey(q => q.CourseId)
                .OnDelete(DeleteBehavior.Restrict);

            // Quiz -> Lesson (nullable)
            modelBuilder.Entity<Quiz>()
                .HasOne(q => q.Lesson)
                .WithMany()
                .HasForeignKey(q => q.LessonId)
                .OnDelete(DeleteBehavior.Restrict);

            // Question -> Quiz
            modelBuilder.Entity<Question>()
                .HasOne(q => q.Quiz)
                .WithMany(qz => qz.Questions)
                .HasForeignKey(q => q.QuizId)
                .OnDelete(DeleteBehavior.Cascade);

            // Option -> Question
            modelBuilder.Entity<Option>()
                .HasOne(o => o.Question)
                .WithMany(q => q.Options)
                .HasForeignKey(o => o.QuestionId)
                .OnDelete(DeleteBehavior.Cascade);

            // QuizAttempt -> Quiz
            modelBuilder.Entity<QuizAttempt>()
                .HasOne(a => a.Quiz)
                .WithMany(q => q.Attempts)
                .HasForeignKey(a => a.QuizId)
                .OnDelete(DeleteBehavior.Cascade);

            // QuizAttempt -> Student (User)
            modelBuilder.Entity<QuizAttempt>()
                .HasOne(a => a.Student)
                .WithMany()
                .HasForeignKey(a => a.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            // Table names
            modelBuilder.Entity<Enrollment>().ToTable("ENROLLMENTS");
            modelBuilder.Entity<LessonProgress>().ToTable("LESSON_PROGRESS");
            modelBuilder.Entity<StudentAcademyFollow>().ToTable("STUDENT_ACADEMY_FOLLOWS");

            // Enrollment -> Student (User)
            modelBuilder.Entity<Enrollment>()
                .HasOne(e => e.Student)
                .WithMany()
                .HasForeignKey(e => e.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            // Enrollment -> Course
            modelBuilder.Entity<Enrollment>()
                .HasOne(e => e.Course)
                .WithMany()
                .HasForeignKey(e => e.CourseId)
                .OnDelete(DeleteBehavior.Restrict);

            // LessonProgress -> Enrollment (cascade — deleting enrollment cleans up progress)
            modelBuilder.Entity<LessonProgress>()
                .HasOne(lp => lp.Enrollment)
                .WithMany(e => e.LessonProgresses)
                .HasForeignKey(lp => lp.EnrollmentId)
                .OnDelete(DeleteBehavior.Cascade);

            // LessonProgress -> Lesson
            modelBuilder.Entity<LessonProgress>()
                .HasOne(lp => lp.Lesson)
                .WithMany()
                .HasForeignKey(lp => lp.LessonId)
                .OnDelete(DeleteBehavior.Restrict);

            // Unique: one progress row per (enrollment, lesson)
            modelBuilder.Entity<LessonProgress>()
                .HasIndex(lp => new { lp.EnrollmentId, lp.LessonId })
                .IsUnique();

            // StudentAcademyFollow -> Student
            modelBuilder.Entity<StudentAcademyFollow>()
                .HasOne(f => f.Student)
                .WithMany()
                .HasForeignKey(f => f.StudentId)
                .OnDelete(DeleteBehavior.Cascade);

            // StudentAcademyFollow -> Academy
            modelBuilder.Entity<StudentAcademyFollow>()
                .HasOne(f => f.Academy)
                .WithMany()
                .HasForeignKey(f => f.AcademyId)
                .OnDelete(DeleteBehavior.Cascade);

            // Unique: one follow per (student, academy)
            modelBuilder.Entity<StudentAcademyFollow>()
                .HasIndex(f => new { f.StudentId, f.AcademyId })
                .IsUnique();

            // Enrollment: no duplicate active enrollment for same (student, course) — enforced at service level
            modelBuilder.Entity<Enrollment>()
                .HasIndex(e => new { e.StudentId, e.CourseId });
        }

    }

}