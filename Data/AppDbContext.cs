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
        public DbSet<Certificate> Certificates => Set<Certificate>();
        public DbSet<Review> Reviews => Set<Review>();
        public DbSet<Comment> Comments => Set<Comment>();

        public DbSet<Plan> Plans => Set<Plan>();
        public DbSet<Subscription> Subscriptions => Set<Subscription>();

        // ─── NEW ───
        public DbSet<Invoice> Invoices => Set<Invoice>();

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

            modelBuilder.Entity<Certificate>().ToTable("CERTIFICATES");

            // Index for lookups (NOT unique — allows reissue to create new cert while keeping history)
            modelBuilder.Entity<Certificate>()
                .HasIndex(c => c.EnrollmentId);

            // Verification code must be globally unique
            modelBuilder.Entity<Certificate>()
                .HasIndex(c => c.VerificationCode)
                .IsUnique();

            // Certificate -> Enrollment
            modelBuilder.Entity<Certificate>()
                .HasOne(c => c.Enrollment)
                .WithMany()
                .HasForeignKey(c => c.EnrollmentId)
                .OnDelete(DeleteBehavior.Restrict);

            // Certificate -> Student (User)
            modelBuilder.Entity<Certificate>()
                .HasOne(c => c.Student)
                .WithMany()
                .HasForeignKey(c => c.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            // Certificate -> Course
            modelBuilder.Entity<Certificate>()
                .HasOne(c => c.Course)
                .WithMany()
                .HasForeignKey(c => c.CourseId)
                .OnDelete(DeleteBehavior.Restrict);

            // Certificate -> Academy
            modelBuilder.Entity<Certificate>()
                .HasOne(c => c.Academy)
                .WithMany()
                .HasForeignKey(c => c.AcademyId)
                .OnDelete(DeleteBehavior.Restrict);

            // Certificate -> IssuedBy (User, nullable)
            modelBuilder.Entity<Certificate>()
                .HasOne(c => c.IssuedBy)
                .WithMany()
                .HasForeignKey(c => c.IssuedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
            // Table name
            modelBuilder.Entity<Review>().ToTable("REVIEWS");

            // One review per enrollment
            modelBuilder.Entity<Review>()
                .HasIndex(r => r.EnrollmentId)
                .IsUnique();

            // Fast lookup by course + status
            modelBuilder.Entity<Review>()
                .HasIndex(r => new { r.CourseId, r.Status });

            // Review -> Course
            modelBuilder.Entity<Review>()
                .HasOne(r => r.Course)
                .WithMany()
                .HasForeignKey(r => r.CourseId)
                .OnDelete(DeleteBehavior.Restrict);

            // Review -> Student (User)
            modelBuilder.Entity<Review>()
                .HasOne(r => r.Student)
                .WithMany()
                .HasForeignKey(r => r.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            // Review -> Enrollment
            modelBuilder.Entity<Review>()
                .HasOne(r => r.Enrollment)
                .WithMany()
                .HasForeignKey(r => r.EnrollmentId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Comment>().ToTable("COMMENTS");

            // Fast lookup: all comments for a target
            modelBuilder.Entity<Comment>()
                .HasIndex(c => new { c.TargetType, c.TargetId });

            // Fast lookup: all comments scoped by course
            modelBuilder.Entity<Comment>()
                .HasIndex(c => c.CourseId);

            // Fast lookup: replies to a parent
            modelBuilder.Entity<Comment>()
                .HasIndex(c => c.ParentCommentId);

            // Comment -> Course
            modelBuilder.Entity<Comment>()
                .HasOne(c => c.Course)
                .WithMany()
                .HasForeignKey(c => c.CourseId)
                .OnDelete(DeleteBehavior.Restrict);

            // Comment -> Author (User)
            modelBuilder.Entity<Comment>()
                .HasOne(c => c.Author)
                .WithMany()
                .HasForeignKey(c => c.AuthorId)
                .OnDelete(DeleteBehavior.Restrict);

            // Comment -> ParentComment (self-referential, no cascade — replies survive parent hide)
            modelBuilder.Entity<Comment>()
                .HasOne(c => c.ParentComment)
                .WithMany()
                .HasForeignKey(c => c.ParentCommentId)
                .OnDelete(DeleteBehavior.Restrict);

            // ─── Plan & Subscription configuration

            modelBuilder.Entity<Plan>().ToTable("PLANS");
            modelBuilder.Entity<Subscription>().ToTable("SUBSCRIPTIONS");

            // Plan slug must be globally unique (used for pricing URL: /pricing/pro)
            modelBuilder.Entity<Plan>()
                .HasIndex(p => p.Slug)
                .IsUnique();

            // One active subscription per academy
            // Enforced here because the notebook says academyId is unique
            modelBuilder.Entity<Subscription>()
                .HasIndex(s => s.AcademyId)
                .IsUnique();

            // Subscription -> Academy
            modelBuilder.Entity<Subscription>()
                .HasOne(s => s.Academy)
                .WithMany()
                .HasForeignKey(s => s.AcademyId)
                .OnDelete(DeleteBehavior.Restrict);

            // Subscription -> Plan
            modelBuilder.Entity<Subscription>()
                .HasOne(s => s.Plan)
                .WithMany(p => p.Subscriptions)
                .HasForeignKey(s => s.PlanId)
                .OnDelete(DeleteBehavior.Restrict);

            // ─── NEW ─── Invoice configuration

            modelBuilder.Entity<Invoice>().ToTable("INVOICES");

            // Invoice number must be globally unique — the retry loop depends on this
            modelBuilder.Entity<Invoice>()
                .HasIndex(i => i.InvoiceNumber)
                .IsUnique();

            // Fast scoping: "all invoices for academy X"
            modelBuilder.Entity<Invoice>()
                .HasIndex(i => i.AcademyId);

            // Fast filtering: "all unpaid invoices", "all overdue invoices"
            modelBuilder.Entity<Invoice>()
                .HasIndex(i => i.Status);

            // Fast lookup: "all invoices for subscription X"
            modelBuilder.Entity<Invoice>()
                .HasIndex(i => i.SubscriptionId);

            // Invoice -> Subscription
            modelBuilder.Entity<Invoice>()
                .HasOne(i => i.Subscription)
                .WithMany(s => s.Invoices)
                .HasForeignKey(i => i.SubscriptionId)
                .OnDelete(DeleteBehavior.Restrict);

            // Invoice -> Academy
            modelBuilder.Entity<Invoice>()
                .HasOne(i => i.Academy)
                .WithMany()
                .HasForeignKey(i => i.AcademyId)
                .OnDelete(DeleteBehavior.Restrict);

            // Invoice -> Plan
            modelBuilder.Entity<Invoice>()
                .HasOne(i => i.Plan)
                .WithMany()
                .HasForeignKey(i => i.PlanId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }

}