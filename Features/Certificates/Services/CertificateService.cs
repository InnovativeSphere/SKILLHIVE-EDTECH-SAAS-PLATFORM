using Microsoft.EntityFrameworkCore;
using SkillHive.Common;
using SkillHive.Data;
using SkillHive.Enums;
using SkillHive.Models;

namespace SkillHive.Features.Certificates.Services
{
    public class CertificateService
    {
        private readonly AppDbContext _db;
        private readonly PdfService _pdfService;
        private readonly CloudinaryService _cloudinary;
        private readonly IConfiguration _config;

        public CertificateService(
            AppDbContext db,
            PdfService pdfService,
            CloudinaryService cloudinary,
            IConfiguration config)
        {
            _db = db;
            _pdfService = pdfService;
            _cloudinary = cloudinary;
            _config = config;
        }

        // ─── Auto-generation on enrollment completion ────────────
        public async Task<Certificate?> GenerateForEnrollmentAsync(int enrollmentId)
        {
            // Return existing if already generated
            var existing = await _db.Certificates
                .FirstOrDefaultAsync(c => c.EnrollmentId == enrollmentId);
            if (existing != null)
                return existing;

            var enrollment = await _db.Enrollments
                .Include(e => e.Student)
                .Include(e => e.Course)
                    .ThenInclude(c => c.Academy)
                .FirstOrDefaultAsync(e => e.EnrollmentId == enrollmentId);

            if (enrollment == null)
                throw new InvalidOperationException("Enrollment not found");

            if (enrollment.Status != EnrollmentStatus.COMPLETED)
                throw new InvalidOperationException("Enrollment is not completed");

            var verificationCode = TokenHelper.GenerateCertificateCode();
            var baseUrl = _config["App:BaseUrl"] ?? "https://skillhive.com";
            var verificationUrl = $"{baseUrl}/verify/{verificationCode}";

            var pdfData = new CertificatePdfData
            {
                StudentName = enrollment.Student.FullName,
                CourseTitle = enrollment.Course.Title,
                AcademyName = enrollment.Course.Academy.Name,
                VerificationCode = verificationCode,
                VerificationUrl = verificationUrl,
                IssuedAt = DateTime.UtcNow
            };

            var pdfBytes = _pdfService.GenerateCertificate(pdfData);
            var fileName = $"certificate_{verificationCode}.pdf";

            using var stream = new MemoryStream(pdfBytes);
            var upload = await _cloudinary.UploadAsync(stream, fileName, "skillhive/certificates");

            var certificate = new Certificate
            {
                EnrollmentId = enrollment.EnrollmentId,
                StudentId = enrollment.StudentId,
                CourseId = enrollment.CourseId,
                AcademyId = enrollment.Course.AcademyId,
                VerificationCode = verificationCode,
                PdfUrl = upload.Url,
                CloudinaryPublicId = upload.PublicId,
                IssuedAt = DateTime.UtcNow,
                Status = CertificateStatus.ISSUED
            };

            _db.Certificates.Add(certificate);

            // Mark enrollment with certificate timestamp
            enrollment.CertificateIssuedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            return certificate;
        }

        // ─── Student: My certificates ────────────────────────────
        // ─── Student: My certificates ────────────────────────────
        public async Task<List<object>> GetMyCertificatesAsync(int studentId)
        {
            var certificates = await _db.Certificates
                .Include(c => c.Course)
                .Include(c => c.Academy)
                .Where(c => c.StudentId == studentId)
                .OrderByDescending(c => c.IssuedAt)
                .Select(c => new
                {
                    certificateId = c.CertificateId,
                    verificationCode = c.VerificationCode,
                    status = c.Status.ToString(),
                    issuedAt = c.IssuedAt,
                    pdfUrl = c.Status == CertificateStatus.REVOKED ? null : c.PdfUrl,
                    course = new
                    {
                        courseId = c.Course.CourseId,
                        title = c.Course.Title,
                        slug = c.Course.Slug
                    },
                    academy = new
                    {
                        academyId = c.Academy.AcademyId,
                        name = c.Academy.Name,
                        slug = c.Academy.Slug
                    }
                })
                .ToListAsync();

            return certificates.Cast<object>().ToList();
        }

        // ─── Get single certificate ──────────────────────────────
        public async Task<object> GetCertificateAsync(int certificateId, int requesterId, UserRole role, int? academyId)
        {
            var certificate = await _db.Certificates
                .Include(c => c.Course)
                .Include(c => c.Academy)
                .Include(c => c.Student)
                .FirstOrDefaultAsync(c => c.CertificateId == certificateId);

            if (certificate == null)
                throw new InvalidOperationException("Certificate not found");

            // Permission: student owns it, or academy staff belongs to same academy
            if (role == UserRole.STUDENT)
            {
                if (certificate.StudentId != requesterId)
                    throw new UnauthorizedAccessException("You can only view your own certificates");
            }
            else if (role == UserRole.ACADEMY_OWNER || role == UserRole.INSTRUCTOR || role == UserRole.MODERATOR)
            {
                if (certificate.AcademyId != academyId)
                    throw new UnauthorizedAccessException("Certificate belongs to another academy");
            }
            else
            {
                throw new UnauthorizedAccessException("You do not have permission to view certificates");
            }

            return new
            {
                certificateId = certificate.CertificateId,
                verificationCode = certificate.VerificationCode,
                status = certificate.Status.ToString(),
                issuedAt = certificate.IssuedAt,
                revokedAt = certificate.RevokedAt,
                revokedReason = certificate.RevokedReason,
                pdfUrl = certificate.PdfUrl,
                course = new
                {
                    courseId = certificate.Course.CourseId,
                    title = certificate.Course.Title,
                    slug = certificate.Course.Slug
                },
                academy = new
                {
                    academyId = certificate.Academy.AcademyId,
                    name = certificate.Academy.Name,
                    slug = certificate.Academy.Slug
                },
                student = new
                {
                    userId = certificate.Student.UserId,
                    fullName = certificate.Student.FullName
                }
            };
        }

        // ─── Public verification ─────────────────────────────────
        public async Task<object> VerifyCertificateAsync(string code)
        {
            var certificate = await _db.Certificates
                .Include(c => c.Course)
                .Include(c => c.Academy)
                .Include(c => c.Student)
                .FirstOrDefaultAsync(c => c.VerificationCode == code);

            if (certificate == null)
                throw new InvalidOperationException("Certificate not found");

            return new
            {
                verified = certificate.Status == CertificateStatus.ISSUED,
                status = certificate.Status.ToString(),
                verificationCode = certificate.VerificationCode,
                studentName = certificate.Student.FullName,
                courseTitle = certificate.Course.Title,
                academyName = certificate.Academy.Name,
                issuedAt = certificate.IssuedAt,
                revokedAt = certificate.RevokedAt,
                revokedReason = certificate.RevokedReason
            };
        }

        // ─── Revoke ──────────────────────────────────────────────
        public async Task<object> RevokeAsync(int certificateId, int requesterId, UserRole role, int? academyId, string reason)
        {
            var certificate = await _db.Certificates
                .FirstOrDefaultAsync(c => c.CertificateId == certificateId);

            if (certificate == null)
                throw new InvalidOperationException("Certificate not found");

            if (role != UserRole.ACADEMY_OWNER && role != UserRole.SUPER_ADMIN)
                throw new UnauthorizedAccessException("Only academy owners or superadmins can revoke");

            if (role == UserRole.ACADEMY_OWNER && certificate.AcademyId != academyId)
                throw new UnauthorizedAccessException("Certificate belongs to another academy");

            if (certificate.Status == CertificateStatus.REVOKED)
                throw new InvalidOperationException("Certificate is already revoked");

            certificate.Status = CertificateStatus.REVOKED;
            certificate.RevokedAt = DateTime.UtcNow;
            certificate.RevokedReason = Utils.SanitizeInput(reason);
            await _db.SaveChangesAsync();

            return new
            {
                certificateId = certificate.CertificateId,
                status = certificate.Status.ToString(),
                revokedAt = certificate.RevokedAt
            };
        }

        // ─── Reissue ─────────────────────────────────────────────
        public async Task<object> ReissueAsync(int certificateId, int requesterId, UserRole role, int? academyId, string? reason)
        {
            var certificate = await _db.Certificates
                .Include(c => c.Student)
                .Include(c => c.Course)
                    .ThenInclude(c => c.Academy)
                .FirstOrDefaultAsync(c => c.CertificateId == certificateId);

            if (certificate == null)
                throw new InvalidOperationException("Certificate not found");

            if (role != UserRole.ACADEMY_OWNER && role != UserRole.SUPER_ADMIN)
                throw new UnauthorizedAccessException("Only academy owners or superadmins can reissue");

            if (role == UserRole.ACADEMY_OWNER && certificate.AcademyId != academyId)
                throw new UnauthorizedAccessException("Certificate belongs to another academy");

            // Mark old as REISSUED
            certificate.Status = CertificateStatus.REISSUED;

            var verificationCode = TokenHelper.GenerateCertificateCode();
            var baseUrl = _config["App:BaseUrl"] ?? "https://skillhive.com";
            var verificationUrl = $"{baseUrl}/verify/{verificationCode}";

            var pdfData = new CertificatePdfData
            {
                StudentName = certificate.Student.FullName,
                CourseTitle = certificate.Course.Title,
                AcademyName = certificate.Course.Academy.Name,
                VerificationCode = verificationCode,
                VerificationUrl = verificationUrl,
                IssuedAt = DateTime.UtcNow
            };

            var pdfBytes = _pdfService.GenerateCertificate(pdfData);
            var fileName = $"certificate_{verificationCode}.pdf";

            using var stream = new MemoryStream(pdfBytes);
            var upload = await _cloudinary.UploadAsync(stream, fileName, "skillhive/certificates");

            var newCertificate = new Certificate
            {
                EnrollmentId = certificate.EnrollmentId,
                StudentId = certificate.StudentId,
                CourseId = certificate.CourseId,
                AcademyId = certificate.AcademyId,
                VerificationCode = verificationCode,
                PdfUrl = upload.Url,
                CloudinaryPublicId = upload.PublicId,
                IssuedAt = DateTime.UtcNow,
                IssuedByUserId = requesterId,
                Status = CertificateStatus.ISSUED
            };

            _db.Certificates.Add(newCertificate);
            await _db.SaveChangesAsync();

            return new
            {
                oldCertificateId = certificate.CertificateId,
                newCertificateId = newCertificate.CertificateId,
                newVerificationCode = newCertificate.VerificationCode,
                reason = reason != null ? Utils.SanitizeInput(reason) : null
            };
        }

        // ─── List academy certificates ───────────────────────────
        // ─── List academy certificates ───────────────────────────
        public async Task<List<object>> ListAcademyCertificatesAsync(int academyId, int? courseId)
        {
            var query = _db.Certificates
                .Include(c => c.Course)
                .Include(c => c.Student)
                .Where(c => c.AcademyId == academyId);

            if (courseId.HasValue)
                query = query.Where(c => c.CourseId == courseId.Value);

            var certificates = await query
                .OrderByDescending(c => c.IssuedAt)
                .Select(c => new
                {
                    certificateId = c.CertificateId,
                    verificationCode = c.VerificationCode,
                    status = c.Status.ToString(),
                    issuedAt = c.IssuedAt,
                    revokedAt = c.RevokedAt,
                    student = new
                    {
                        userId = c.Student.UserId,
                        fullName = c.Student.FullName,
                        email = c.Student.Email
                    },
                    course = new
                    {
                        courseId = c.Course.CourseId,
                        title = c.Course.Title
                    }
                })
                .ToListAsync();

            return certificates.Cast<object>().ToList();
        }
        public async Task<object> RegenerateForEnrollmentAsync(int enrollmentId, int requesterId, UserRole role, int? academyId)
        {
            var enrollment = await _db.Enrollments
                .Include(e => e.Course)
                .FirstOrDefaultAsync(e => e.EnrollmentId == enrollmentId);

            if (enrollment == null)
                throw new InvalidOperationException("Enrollment not found");

            if (enrollment.Status != EnrollmentStatus.COMPLETED)
                throw new InvalidOperationException("Enrollment is not completed");

            if (role != UserRole.ACADEMY_OWNER && role != UserRole.SUPER_ADMIN)
                throw new UnauthorizedAccessException("Only academy owners or superadmins can regenerate certificates");

            if (role == UserRole.ACADEMY_OWNER && enrollment.Course.AcademyId != academyId)
                throw new UnauthorizedAccessException("Enrollment belongs to another academy");

            // Wipe any existing cert (in case of partial failure)
            var existing = await _db.Certificates
                .FirstOrDefaultAsync(c => c.EnrollmentId == enrollmentId);

            if (existing != null)
            {
                _db.Certificates.Remove(existing);
                await _db.SaveChangesAsync();
            }

            var cert = await GenerateForEnrollmentAsync(enrollmentId);
            return new
            {
                certificateId = cert!.CertificateId,
                verificationCode = cert.VerificationCode,
                pdfUrl = cert.PdfUrl,
                status = cert.Status.ToString()
            };
        }
    }

}