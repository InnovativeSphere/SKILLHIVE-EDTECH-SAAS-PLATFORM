using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SkillHive.Common;
using SkillHive.Enums;
using SkillHive.Features.Certificates.DTOs;
using SkillHive.Features.Certificates.Services;

namespace SkillHive.Features.Certificates.Controllers
{
    [ApiController]
    [Route("api/certificates")]
    public class CertificatesController : ControllerBase
    {
        private readonly CertificateService _certificateService;

        public CertificatesController(CertificateService certificateService)
        {
            _certificateService = certificateService;
        }

        // ─── Public verification ─────────────────────────────────
        [HttpGet("verify/{code}")]
        public async Task<IActionResult> Verify(string code)
        {
            try
            {
                var result = await _certificateService.VerifyCertificateAsync(code);
                return ApiResponse.Success(result);
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

        // ─── My certificates (Student) ───────────────────────────
        [HttpGet("my")]
        [Authorize(Roles = "STUDENT")]
        public async Task<IActionResult> GetMyCertificates()
        {
            try
            {
                var userId = JwtHelper.GetUserId(User);
                var result = await _certificateService.GetMyCertificatesAsync(userId);
                return ApiResponse.Success(result);
            }
            catch (Exception)
            {
                return ApiResponse.Error();
            }
        }

        // ─── Academy certificates (Owner/Instructor) ─────────────
        [HttpGet("academy")]
        [Authorize(Roles = "ACADEMY_OWNER,INSTRUCTOR,MODERATOR")]
        public async Task<IActionResult> ListAcademyCertificates([FromQuery] int? courseId)
        {
            try
            {
                var academyId = JwtHelper.GetAcademyId(User);
                if (academyId == null) return ApiResponse.Forbidden("You do not belong to an academy");

                var result = await _certificateService.ListAcademyCertificatesAsync(academyId.Value, courseId);
                return ApiResponse.Success(result);
            }
            catch (Exception)
            {
                return ApiResponse.Error();
            }
        }

        // ─── Get single certificate ──────────────────────────────
        [HttpGet("{id:int}")]
        [Authorize]
        public async Task<IActionResult> GetCertificate(int id)
        {
            try
            {
                var userId = JwtHelper.GetUserId(User);
                var roleStr = JwtHelper.GetRole(User);
                var role = Enum.TryParse<UserRole>(roleStr, out var r) ? r : UserRole.STUDENT;
                var academyId = JwtHelper.GetAcademyId(User);

                var result = await _certificateService.GetCertificateAsync(id, userId, role, academyId);
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

        // ─── Revoke ──────────────────────────────────────────────
        [HttpPost("{id:int}/revoke")]
        [Authorize(Roles = "ACADEMY_OWNER,SUPER_ADMIN")]
        public async Task<IActionResult> Revoke(int id, [FromBody] RevokeCertificateDto dto)
        {
            try
            {
                var userId = JwtHelper.GetUserId(User);
                var roleStr = JwtHelper.GetRole(User);
                var role = Enum.TryParse<UserRole>(roleStr, out var r) ? r : UserRole.STUDENT;
                var academyId = JwtHelper.GetAcademyId(User);

                var result = await _certificateService.RevokeAsync(id, userId, role, academyId, dto.Reason);
                return ApiResponse.Success(result, "Certificate revoked");
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

        // ─── Reissue ─────────────────────────────────────────────
        [HttpPost("{id:int}/reissue")]
        [Authorize(Roles = "ACADEMY_OWNER,SUPER_ADMIN")]
        public async Task<IActionResult> Reissue(int id, [FromBody] ReissueCertificateDto dto)
        {
            try
            {
                var userId = JwtHelper.GetUserId(User);
                var roleStr = JwtHelper.GetRole(User);
                var role = Enum.TryParse<UserRole>(roleStr, out var r) ? r : UserRole.STUDENT;
                var academyId = JwtHelper.GetAcademyId(User);

                var result = await _certificateService.ReissueAsync(id, userId, role, academyId, dto.Reason);
                return ApiResponse.Created(result, "Certificate reissued");
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

        [HttpPost("regenerate/{enrollmentId:int}")]
        [Authorize(Roles = "ACADEMY_OWNER,SUPER_ADMIN")]
        public async Task<IActionResult> Regenerate(int enrollmentId)
        {
            try
            {
                var userId = JwtHelper.GetUserId(User);
                var roleStr = JwtHelper.GetRole(User);
                var role = Enum.TryParse<UserRole>(roleStr, out var r) ? r : UserRole.STUDENT;
                var academyId = JwtHelper.GetAcademyId(User);

                var result = await _certificateService.RegenerateForEnrollmentAsync(enrollmentId, userId, role, academyId);
                return ApiResponse.Created(result, "Certificate regenerated");
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
    }
}