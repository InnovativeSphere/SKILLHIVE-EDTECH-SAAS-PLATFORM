using Microsoft.EntityFrameworkCore;
using SkillHive.Common;
using SkillHive.Data;
using SkillHive.Enums;
using SkillHive.Features.Quizzes.DTOs;
using SkillHive.Models;

namespace SkillHive.Features.Quizzes.Services
{
    public class QuizService
    {
        private readonly AppDbContext _db;

        public QuizService(AppDbContext db)
        {
            _db = db;
        }

        // ─── Get Quiz (for staff: includes options with isCorrect) ──
        public async Task<object> GetQuizAsync(int quizId, bool isStaff, int? requesterAcademyId, int requesterUserId)
        {
            var quiz = await _db.Quizzes
                .Include(q => q.Course)
                .Include(q => q.Lesson)
                .FirstOrDefaultAsync(q => q.QuizId == quizId);

            if (quiz == null)
                throw new InvalidOperationException("Quiz not found");

            // Permission check for staff access
            if (isStaff)
                await EnsureCanAccessQuizAsync(quiz, requesterAcademyId);

            // Load questions + options
            var questions = await _db.Questions
                .Where(q => q.QuizId == quizId && q.IsActive)
                .OrderBy(q => q.Order)
                .Select(q => new
                {
                    questionId = q.QuestionId,
                    questionText = q.QuestionText,
                    points = q.Points,
                    order = q.Order,
                    options = q.Options
                        .OrderBy(o => o.Order)
                        .Select(o => isStaff
                            ? (object)new
                            {
                                optionId = o.OptionId,
                                optionText = o.OptionText,
                                isCorrect = o.IsCorrect,
                                order = o.Order
                            }
                            : new
                            {
                                optionId = o.OptionId,
                                optionText = o.OptionText,
                                order = o.Order
                            })
                        .ToList()
                })
                .ToListAsync();

            return new
            {
                quizId = quiz.QuizId,
                courseId = quiz.CourseId,
                lessonId = quiz.LessonId,
                title = quiz.Title,
                description = quiz.Description,
                passingScore = quiz.PassingScore,
                timeLimitMinutes = quiz.TimeLimitMinutes,
                maxAttempts = quiz.MaxAttempts,
                cooldownMinutes = quiz.CooldownMinutes,
                createdAt = quiz.CreatedAt,
                updatedAt = quiz.UpdatedAt,
                questions
            };
        }

        // ─── Get Quiz By Lesson (public/staff) ──────────────────────
        public async Task<object> GetQuizByLessonAsync(int lessonId, bool isStaff, int? requesterAcademyId, int requesterUserId)
        {
            var quiz = await _db.Quizzes
                .Include(q => q.Lesson)
                    .ThenInclude(l => l!.Course)
                .FirstOrDefaultAsync(q => q.LessonId == lessonId);

            if (quiz == null)
                throw new InvalidOperationException("Quiz not found for this lesson");

            return await GetQuizAsync(quiz.QuizId, isStaff, requesterAcademyId, requesterUserId);
        }

        // ─── Create Quiz ────────────────────────────────────────────
        public async Task<object> CreateQuizAsync(
            int userId, UserRole role, int? academyId, CreateQuizDto dto)
        {
            // Exactly one of CourseId or LessonId
            if (dto.CourseId.HasValue == dto.LessonId.HasValue)
                throw new InvalidOperationException("Provide exactly one of courseId or lessonId");

            Course course;

            if (dto.CourseId.HasValue)
            {
                var found = await _db.Courses.FirstOrDefaultAsync(c => c.CourseId == dto.CourseId.Value);
                if (found == null) throw new InvalidOperationException("Course not found");
                course = found;
            }
            else
            {
                var lesson = await _db.Lessons
                    .Include(l => l.Course)
                    .FirstOrDefaultAsync(l => l.LessonId == dto.LessonId!.Value);
                if (lesson == null) throw new InvalidOperationException("Lesson not found");
                course = lesson.Course;
            }

            await EnsureCanModifyCourseAsync(userId, role, academyId, course);

            var quiz = new Quiz
            {
                CourseId = dto.CourseId,
                LessonId = dto.LessonId,
                Title = Utils.ToTitleCase(Utils.SanitizeInput(dto.Title)),
                Description = dto.Description != null ? Utils.SanitizeInput(dto.Description) : null,
                PassingScore = dto.PassingScore,
                TimeLimitMinutes = dto.TimeLimitMinutes,
                MaxAttempts = dto.MaxAttempts,
                CooldownMinutes = dto.CooldownMinutes,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _db.Quizzes.Add(quiz);
            course.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            return new
            {
                quizId = quiz.QuizId,
                courseId = quiz.CourseId,
                lessonId = quiz.LessonId,
                title = quiz.Title,
                passingScore = quiz.PassingScore,
                maxAttempts = quiz.MaxAttempts,
                cooldownMinutes = quiz.CooldownMinutes,
                createdAt = quiz.CreatedAt
            };
        }

        // ─── Update Quiz ────────────────────────────────────────────
        public async Task<object> UpdateQuizAsync(
            int userId, UserRole role, int? academyId, int quizId, UpdateQuizDto dto)
        {
            var quiz = await GetQuizForModificationAsync(userId, role, academyId, quizId);

            if (!string.IsNullOrWhiteSpace(dto.Title))
                quiz.Title = Utils.ToTitleCase(Utils.SanitizeInput(dto.Title));

            if (dto.Description != null)
                quiz.Description = Utils.SanitizeInput(dto.Description);

            if (dto.PassingScore.HasValue) quiz.PassingScore = dto.PassingScore.Value;
            if (dto.TimeLimitMinutes.HasValue) quiz.TimeLimitMinutes = dto.TimeLimitMinutes.Value;
            if (dto.MaxAttempts.HasValue) quiz.MaxAttempts = dto.MaxAttempts.Value;
            if (dto.CooldownMinutes.HasValue) quiz.CooldownMinutes = dto.CooldownMinutes.Value;

            quiz.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            return new
            {
                quizId = quiz.QuizId,
                title = quiz.Title,
                passingScore = quiz.PassingScore,
                maxAttempts = quiz.MaxAttempts,
                cooldownMinutes = quiz.CooldownMinutes,
                updatedAt = quiz.UpdatedAt
            };
        }

        // ─── Deactivate Quiz (soft delete) ──────────────────────────
        public async Task<object> DeactivateQuizAsync(
            int userId, UserRole role, int? academyId, int quizId)
        {
            var quiz = await GetQuizForModificationAsync(userId, role, academyId, quizId);

            // Block if any questions exist and are active — force explicit cleanup first
            var activeQuestions = await _db.Questions
                .AnyAsync(q => q.QuizId == quizId && q.IsActive);

            if (activeQuestions)
                throw new InvalidOperationException(
                    "Deactivate all questions first before deactivating the quiz");

            // Soft delete by severing parent link is not possible — we must keep the row.
            // Instead, we detach from both parents so no student sees it. But since one of
            // CourseId/LessonId must be set, we keep it attached and mark quiz as having
            // no active questions — which is effectively deactivated. Cleaner alternative:
            // we throw if there are questions, otherwise we simply delete the row.
            _db.Quizzes.Remove(quiz);
            await _db.SaveChangesAsync();

            return new { quizId, message = "Quiz deleted" };
        }

        // ─── Create Question (with options) ────────────────────────
        public async Task<object> CreateQuestionAsync(
            int userId, UserRole role, int? academyId, int quizId, CreateQuestionDto dto)
        {
            var quiz = await GetQuizForModificationAsync(userId, role, academyId, quizId);

            ValidateOptions(dto.Options);

            int targetOrder;

            if (dto.Order.HasValue && dto.Order.Value > 0)
            {
                targetOrder = dto.Order.Value;

                var toShift = await _db.Questions
                    .Where(q => q.QuizId == quizId && q.Order >= targetOrder)
                    .ToListAsync();

                foreach (var q in toShift) q.Order += 1;
            }
            else
            {
                var maxOrder = await _db.Questions
                    .Where(q => q.QuizId == quizId)
                    .MaxAsync(q => (int?)q.Order) ?? 0;

                targetOrder = maxOrder + 1;
            }

            var question = new Question
            {
                QuizId = quizId,
                QuestionText = Utils.SanitizeInput(dto.QuestionText),
                Points = dto.Points,
                Order = targetOrder,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _db.Questions.Add(question);
            await _db.SaveChangesAsync();

            var optionOrder = 1;
            foreach (var optDto in dto.Options)
            {
                _db.Options.Add(new Option
                {
                    QuestionId = question.QuestionId,
                    OptionText = Utils.SanitizeInput(optDto.OptionText),
                    IsCorrect = optDto.IsCorrect,
                    Order = optDto.Order ?? optionOrder
                });
                optionOrder++;
            }

            quiz.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            return new
            {
                questionId = question.QuestionId,
                quizId = question.QuizId,
                questionText = question.QuestionText,
                points = question.Points,
                order = question.Order,
                optionsCount = dto.Options.Count,
                createdAt = question.CreatedAt
            };
        }

        // ─── Update Question (with full options replacement) ──────
        public async Task<object> UpdateQuestionAsync(
            int userId, UserRole role, int? academyId, int questionId, UpdateQuestionDto dto)
        {
            var question = await _db.Questions
                .Include(q => q.Quiz)
                .FirstOrDefaultAsync(q => q.QuestionId == questionId);

            if (question == null)
                throw new InvalidOperationException("Question not found");

            await EnsureCanModifyQuizOwnerAsync(userId, role, academyId, question.Quiz);

            if (!string.IsNullOrWhiteSpace(dto.QuestionText))
                question.QuestionText = Utils.SanitizeInput(dto.QuestionText);

            if (dto.Points.HasValue) question.Points = dto.Points.Value;

            if (dto.Options != null)
            {
                ValidateOptions(dto.Options);

                // Replace all options
                var existing = await _db.Options
                    .Where(o => o.QuestionId == questionId)
                    .ToListAsync();

                _db.Options.RemoveRange(existing);

                var optionOrder = 1;
                foreach (var optDto in dto.Options)
                {
                    _db.Options.Add(new Option
                    {
                        QuestionId = questionId,
                        OptionText = Utils.SanitizeInput(optDto.OptionText),
                        IsCorrect = optDto.IsCorrect,
                        Order = optDto.Order ?? optionOrder
                    });
                    optionOrder++;
                }
            }

            if (dto.Order.HasValue && dto.Order.Value > 0 && dto.Order.Value != question.Order)
            {
                var oldOrder = question.Order;
                var newOrder = dto.Order.Value;
                var quizId = question.QuizId;

                if (newOrder > oldOrder)
                {
                    var affected = await _db.Questions
                        .Where(q => q.QuizId == quizId
                                    && q.Order > oldOrder
                                    && q.Order <= newOrder
                                    && q.QuestionId != questionId)
                        .ToListAsync();

                    foreach (var q in affected) q.Order -= 1;
                }
                else
                {
                    var affected = await _db.Questions
                        .Where(q => q.QuizId == quizId
                                    && q.Order >= newOrder
                                    && q.Order < oldOrder
                                    && q.QuestionId != questionId)
                        .ToListAsync();

                    foreach (var q in affected) q.Order += 1;
                }

                question.Order = newOrder;
            }

            question.Quiz.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            return new
            {
                questionId = question.QuestionId,
                questionText = question.QuestionText,
                points = question.Points,
                order = question.Order
            };
        }

        // ─── Deactivate Question ───────────────────────────────────
        public async Task<object> DeactivateQuestionAsync(
            int userId, UserRole role, int? academyId, int questionId)
        {
            var question = await _db.Questions
                .Include(q => q.Quiz)
                .FirstOrDefaultAsync(q => q.QuestionId == questionId);

            if (question == null)
                throw new InvalidOperationException("Question not found");

            await EnsureCanModifyQuizOwnerAsync(userId, role, academyId, question.Quiz);

            // Block if attempts exist on this quiz — historical fairness
            var attemptsExist = await _db.QuizAttempts
                .AnyAsync(a => a.QuizId == question.QuizId);

            if (attemptsExist)
                throw new InvalidOperationException(
                    "Cannot modify questions after students have attempted the quiz");

            question.IsActive = false;

            // Close the gap
            var after = await _db.Questions
                .Where(q => q.QuizId == question.QuizId && q.Order > question.Order)
                .ToListAsync();

            foreach (var q in after) q.Order -= 1;

            question.Quiz.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            return new { questionId = question.QuestionId, isActive = false, message = "Question deactivated" };
        }

        // ─── Submit Attempt (the grading core) ──────────────────────
        public async Task<object> SubmitAttemptAsync(
            int userId, UserRole role, int quizId, SubmitQuizDto dto)
        {
            if (role != UserRole.STUDENT)
                throw new UnauthorizedAccessException("Only students can submit quiz attempts");

            var quiz = await _db.Quizzes.FirstOrDefaultAsync(q => q.QuizId == quizId);
            if (quiz == null)
                throw new InvalidOperationException("Quiz not found");

            // Cooldown check
            var lastAttempt = await _db.QuizAttempts
                .Where(a => a.QuizId == quizId && a.StudentId == userId)
                .OrderByDescending(a => a.StartedAt)
                .FirstOrDefaultAsync();

            if (lastAttempt != null && quiz.CooldownMinutes.HasValue)
            {
                var cooldownEnds = lastAttempt.StartedAt.AddMinutes(quiz.CooldownMinutes.Value);
                if (DateTime.UtcNow < cooldownEnds)
                {
                    var minutesLeft = (int)Math.Ceiling((cooldownEnds - DateTime.UtcNow).TotalMinutes);
                    throw new InvalidOperationException(
                        $"Please wait {minutesLeft} more minute(s) before retrying");
                }
            }

            // Max attempts check
            if (quiz.MaxAttempts.HasValue)
            {
                var attemptCount = await _db.QuizAttempts
                    .CountAsync(a => a.QuizId == quizId && a.StudentId == userId);

                if (attemptCount >= quiz.MaxAttempts.Value)
                    throw new InvalidOperationException(
                        "You have reached the maximum number of attempts for this quiz");
            }

            // Load questions with correct options
            var questions = await _db.Questions
                .Where(q => q.QuizId == quizId && q.IsActive)
                .Include(q => q.Options)
                .ToListAsync();

            if (questions.Count == 0)
                throw new InvalidOperationException("This quiz has no questions yet");

            var totalPoints = questions.Sum(q => q.Points);
            var earnedPoints = 0;

            foreach (var question in questions)
            {
                var submitted = dto.Answers.FirstOrDefault(a => a.QuestionId == question.QuestionId);
                if (submitted?.OptionId == null) continue;

                var correctOption = question.Options.FirstOrDefault(o => o.IsCorrect);
                if (correctOption != null && correctOption.OptionId == submitted.OptionId.Value)
                    earnedPoints += question.Points;
            }

            var score = totalPoints > 0
                ? (int)Math.Round((double)earnedPoints / totalPoints * 100)
                : 0;

            var passed = score >= quiz.PassingScore;

            var attempt = new QuizAttempt
            {
                QuizId = quizId,
                StudentId = userId,
                Score = score,
                Passed = passed,
                StartedAt = DateTime.UtcNow,
                CompletedAt = DateTime.UtcNow
            };

            _db.QuizAttempts.Add(attempt);
            await _db.SaveChangesAsync();

            return new
            {
                attemptId = attempt.AttemptId,
                quizId = quizId,
                score,
                passingScore = quiz.PassingScore,
                passed,
                earnedPoints,
                totalPoints,
                completedAt = attempt.CompletedAt
            };
        }

        // ─── Get Attempt History (student) ──────────────────────────
        public async Task<List<object>> GetMyAttemptsAsync(int userId, int quizId)
        {
            var attempts = await _db.QuizAttempts
                .Where(a => a.QuizId == quizId && a.StudentId == userId)
                .OrderByDescending(a => a.StartedAt)
                .Select(a => new
                {
                    attemptId = a.AttemptId,
                    score = a.Score,
                    passed = a.Passed,
                    startedAt = a.StartedAt,
                    completedAt = a.CompletedAt
                })
                .ToListAsync();

            return attempts.Cast<object>().ToList();
        }

        // ─── Helpers ────────────────────────────────────────────────

        private static void ValidateOptions(List<CreateOptionDto> options)
        {
            if (options == null || options.Count < 2)
                throw new InvalidOperationException("A question must have at least 2 options");

            var correctCount = options.Count(o => o.IsCorrect);
            if (correctCount == 0)
                throw new InvalidOperationException("A question must have at least one correct option");
        }

        private async Task<Quiz> GetQuizForModificationAsync(
            int userId, UserRole role, int? academyId, int quizId)
        {
            var quiz = await _db.Quizzes
                .Include(q => q.Course)
                .Include(q => q.Lesson)
                    .ThenInclude(l => l!.Course)
                .FirstOrDefaultAsync(q => q.QuizId == quizId);

            if (quiz == null)
                throw new InvalidOperationException("Quiz not found");

            await EnsureCanModifyQuizOwnerAsync(userId, role, academyId, quiz);
            return quiz;
        }

        private async Task EnsureCanModifyQuizOwnerAsync(
            int userId, UserRole role, int? academyId, Quiz quiz)
        {
            // Resolve the course the quiz belongs to
            var course = quiz.Course
                ?? await _db.Courses.FirstOrDefaultAsync(c => c.CourseId == quiz.Lesson!.CourseId);

            if (course == null)
                throw new InvalidOperationException("Quiz has no resolvable course");

            await EnsureCanModifyCourseAsync(userId, role, academyId, course);
        }

        private Task EnsureCanModifyCourseAsync(
            int userId, UserRole role, int? academyId, Course course)
        {
            if (role == UserRole.ACADEMY_OWNER)
            {
                if (course.AcademyId != academyId)
                    throw new UnauthorizedAccessException("Course does not belong to your academy");
                return Task.CompletedTask;
            }

            if (role == UserRole.INSTRUCTOR)
            {
                if (course.InstructorId != userId)
                    throw new UnauthorizedAccessException("You can only modify quizzes of courses you created");
                return Task.CompletedTask;
            }

            throw new UnauthorizedAccessException("You do not have permission to modify quizzes");
        }

        private async Task EnsureCanAccessQuizAsync(Quiz quiz, int? requesterAcademyId)
        {
            var course = quiz.Course
                ?? await _db.Courses.FirstOrDefaultAsync(c => c.CourseId == quiz.Lesson!.CourseId);

            if (course == null)
                throw new InvalidOperationException("Quiz has no resolvable course");

            if (course.AcademyId != requesterAcademyId)
                throw new UnauthorizedAccessException("Quiz belongs to another academy");
        }
    }
}