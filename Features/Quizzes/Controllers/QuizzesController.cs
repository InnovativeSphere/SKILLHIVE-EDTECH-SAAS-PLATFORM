using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SkillHive.Common;
using SkillHive.Enums;
using SkillHive.Features.Quizzes.DTOs;
using SkillHive.Features.Quizzes.Services;

namespace SkillHive.Features.Quizzes.Controllers
{
    [ApiController]
    [Route("api/quizzes")]
    public class QuizzesController : ControllerBase
    {
        private readonly QuizService _quizService;

        public QuizzesController(QuizService quizService)
        {
            _quizService = quizService;
        }

        // ─── Get Quiz by ID ────────────────────────────────────────
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetQuiz(int id)
        {
            try
            {
                var (isStaff, academyId) = GetStaffContext();

                var result = await _quizService.GetQuizAsync(id, isStaff, academyId, JwtHelper.GetUserId(User));
                return ApiResponse.Success(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return ApiResponse.Forbidden(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return ApiResponse.NotFound(ex.Message);
            }
            catch (Exception)
            {
                return ApiResponse.Error();
            }
        }

        // ─── Get Quiz by Lesson (public path) ──────────────────────
        [HttpGet("by-lesson/{lessonId:int}")]
        public async Task<IActionResult> GetQuizByLesson(int lessonId)
        {
            try
            {
                var (isStaff, academyId) = GetStaffContext();

                var result = await _quizService.GetQuizByLessonAsync(lessonId, isStaff, academyId, JwtHelper.GetUserId(User));
                return ApiResponse.Success(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return ApiResponse.Forbidden(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return ApiResponse.NotFound(ex.Message);
            }
            catch (Exception)
            {
                return ApiResponse.Error();
            }
        }

        // ─── Create Quiz ───────────────────────────────────────────
        [HttpPost]
        [Authorize(Roles = "ACADEMY_OWNER,INSTRUCTOR")]
        public async Task<IActionResult> Create([FromBody] CreateQuizDto dto)
        {
            try
            {
                var userId = JwtHelper.GetUserId(User);
                var role = ParseRole();
                var academyId = JwtHelper.GetAcademyId(User);

                var result = await _quizService.CreateQuizAsync(userId, role, academyId, dto);
                return ApiResponse.Created(result, "Quiz created");
            }
            catch (UnauthorizedAccessException ex)
            {
                return ApiResponse.Forbidden(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return ApiResponse.BadRequest(ex.Message);
            }
            catch (Exception)
            {
                return ApiResponse.Error();
            }
        }

        // ─── Update Quiz ───────────────────────────────────────────
        [HttpPatch("{id:int}")]
        [Authorize(Roles = "ACADEMY_OWNER,INSTRUCTOR")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateQuizDto dto)
        {
            try
            {
                var userId = JwtHelper.GetUserId(User);
                var role = ParseRole();
                var academyId = JwtHelper.GetAcademyId(User);

                var result = await _quizService.UpdateQuizAsync(userId, role, academyId, id, dto);
                return ApiResponse.Success(result, "Quiz updated");
            }
            catch (UnauthorizedAccessException ex)
            {
                return ApiResponse.Forbidden(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return ApiResponse.BadRequest(ex.Message);
            }
            catch (Exception)
            {
                return ApiResponse.Error();
            }
        }

        // ─── Delete Quiz ───────────────────────────────────────────
        [HttpDelete("{id:int}")]
        [Authorize(Roles = "ACADEMY_OWNER,INSTRUCTOR")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var userId = JwtHelper.GetUserId(User);
                var role = ParseRole();
                var academyId = JwtHelper.GetAcademyId(User);

                var result = await _quizService.DeactivateQuizAsync(userId, role, academyId, id);
                return ApiResponse.Success(result, "Quiz deleted");
            }
            catch (UnauthorizedAccessException ex)
            {
                return ApiResponse.Forbidden(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return ApiResponse.BadRequest(ex.Message);
            }
            catch (Exception)
            {
                return ApiResponse.Error();
            }
        }

        // ─── Create Question ───────────────────────────────────────
        [HttpPost("{quizId:int}/questions")]
        [Authorize(Roles = "ACADEMY_OWNER,INSTRUCTOR")]
        public async Task<IActionResult> CreateQuestion(int quizId, [FromBody] CreateQuestionDto dto)
        {
            try
            {
                var userId = JwtHelper.GetUserId(User);
                var role = ParseRole();
                var academyId = JwtHelper.GetAcademyId(User);

                var result = await _quizService.CreateQuestionAsync(userId, role, academyId, quizId, dto);
                return ApiResponse.Created(result, "Question created");
            }
            catch (UnauthorizedAccessException ex)
            {
                return ApiResponse.Forbidden(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return ApiResponse.BadRequest(ex.Message);
            }
            catch (Exception)
            {
                return ApiResponse.Error();
            }
        }

        // ─── Update Question ───────────────────────────────────────
        [HttpPatch("questions/{questionId:int}")]
        [Authorize(Roles = "ACADEMY_OWNER,INSTRUCTOR")]
        public async Task<IActionResult> UpdateQuestion(int questionId, [FromBody] UpdateQuestionDto dto)
        {
            try
            {
                var userId = JwtHelper.GetUserId(User);
                var role = ParseRole();
                var academyId = JwtHelper.GetAcademyId(User);

                var result = await _quizService.UpdateQuestionAsync(userId, role, academyId, questionId, dto);
                return ApiResponse.Success(result, "Question updated");
            }
            catch (UnauthorizedAccessException ex)
            {
                return ApiResponse.Forbidden(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return ApiResponse.BadRequest(ex.Message);
            }
            catch (Exception)
            {
                return ApiResponse.Error();
            }
        }

        // ─── Deactivate Question ───────────────────────────────────
        [HttpDelete("questions/{questionId:int}")]
        [Authorize(Roles = "ACADEMY_OWNER,INSTRUCTOR")]
        public async Task<IActionResult> DeactivateQuestion(int questionId)
        {
            try
            {
                var userId = JwtHelper.GetUserId(User);
                var role = ParseRole();
                var academyId = JwtHelper.GetAcademyId(User);

                var result = await _quizService.DeactivateQuestionAsync(userId, role, academyId, questionId);
                return ApiResponse.Success(result, "Question deactivated");
            }
            catch (UnauthorizedAccessException ex)
            {
                return ApiResponse.Forbidden(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return ApiResponse.BadRequest(ex.Message);
            }
            catch (Exception)
            {
                return ApiResponse.Error();
            }
        }

        // ─── Submit Attempt (Student) ──────────────────────────────
        [HttpPost("{quizId:int}/submit")]
        [Authorize(Roles = "STUDENT")]
        public async Task<IActionResult> Submit(int quizId, [FromBody] SubmitQuizDto dto)
        {
            try
            {
                var userId = JwtHelper.GetUserId(User);
                var role = ParseRole();

                var result = await _quizService.SubmitAttemptAsync(userId, role, quizId, dto);
                return ApiResponse.Created(result, "Attempt submitted");
            }
            catch (UnauthorizedAccessException ex)
            {
                return ApiResponse.Forbidden(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return ApiResponse.BadRequest(ex.Message);
            }
            catch (Exception)
            {
                return ApiResponse.Error();
            }
        }

        // ─── My Attempt History ────────────────────────────────────
        [HttpGet("{quizId:int}/my-attempts")]
        [Authorize(Roles = "STUDENT")]
        public async Task<IActionResult> MyAttempts(int quizId)
        {
            try
            {
                var userId = JwtHelper.GetUserId(User);
                var result = await _quizService.GetMyAttemptsAsync(userId, quizId);
                return ApiResponse.Success(result);
            }
            catch (Exception)
            {
                return ApiResponse.Error();
            }
        }

        // ─── Helpers ───────────────────────────────────────────────
        private UserRole ParseRole()
        {
            var roleStr = JwtHelper.GetRole(User);
            return Enum.TryParse<UserRole>(roleStr, out var role) ? role : UserRole.STUDENT;
        }

        private (bool isStaff, int? academyId) GetStaffContext()
        {
            var roleStr = JwtHelper.GetRole(User);
            var isStaff = roleStr == UserRole.ACADEMY_OWNER.ToString()
                          || roleStr == UserRole.INSTRUCTOR.ToString()
                          || roleStr == UserRole.MODERATOR.ToString();

            var academyId = JwtHelper.GetAcademyId(User);
            return (isStaff, academyId);
        }
    }
}