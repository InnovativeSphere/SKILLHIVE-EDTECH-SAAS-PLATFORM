using Microsoft.EntityFrameworkCore;
using SkillHive.Common;
using SkillHive.Data;
using SkillHive.Features.Academies.DTOs;

namespace SkillHive.Features.Academies.Services
{
    public class AcademyService
    {
        private readonly AppDbContext _db;

        public AcademyService(AppDbContext db)
        {
            _db = db;
        }

        public async Task<object> GetPublicProfileAsync(string slug)
        {
            var academy = await _db.Academies
                .Where(a => a.Slug == slug && a.IsActive)
                .Select(a => new
                {
                    academyId = a.AcademyId,
                    name = a.Name,
                    slug = a.Slug,
                    description = a.Description,
                    logoUrl = a.LogoUrl,
                    bannerUrl = a.BannerUrl,
                    isVerified = a.IsVerified,
                    createdAt = a.CreatedAt
                })
                .FirstOrDefaultAsync();

            if (academy == null)
                throw new InvalidOperationException("Academy not found");

            return academy;
        }

        public async Task<object> GetOwnAcademyAsync(int userId)
        {
            var academy = await _db.Academies
                .Where(a => a.OwnerId == userId)
                .Select(a => new
                {
                    academyId = a.AcademyId,
                    name = a.Name,
                    slug = a.Slug,
                    description = a.Description,
                    logoUrl = a.LogoUrl,
                    bannerUrl = a.BannerUrl,
                    email = a.Email,
                    phone = a.Phone,
                    address = a.Address,
                    ownerId = a.OwnerId,
                    isVerified = a.IsVerified,
                    isActive = a.IsActive,
                    createdAt = a.CreatedAt,
                    updatedAt = a.UpdatedAt
                })
                .FirstOrDefaultAsync();

            if (academy == null)
                throw new InvalidOperationException("You do not own an academy");

            return academy;
        }

        public async Task<object> UpdateOwnAcademyAsync(int userId, UpdateAcademyDto dto)
        {
            var academy = await _db.Academies
                .FirstOrDefaultAsync(a => a.OwnerId == userId);

            if (academy == null)
                throw new InvalidOperationException("You do not own an academy");

            if (!string.IsNullOrWhiteSpace(dto.Name))
            {
                academy.Name = Utils.ToTitleCase(Utils.SanitizeInput(dto.Name));

                var newSlug = SlugHelper.GenerateSlug(academy.Name);
                if (newSlug != academy.Slug)
                {
                    var uniqueSlug = await SlugHelper.EnsureUniqueSlugAsync(
                        newSlug,
                        async s => await _db.Academies.AnyAsync(a => a.Slug == s && a.AcademyId != academy.AcademyId));

                    academy.Slug = uniqueSlug;
                }
            }

            if (dto.Description != null)
                academy.Description = Utils.SanitizeInput(dto.Description);

            if (dto.LogoUrl != null)
                academy.LogoUrl = dto.LogoUrl;

            if (dto.BannerUrl != null)
                academy.BannerUrl = dto.BannerUrl;

            if (dto.Email != null)
                academy.Email = dto.Email.ToLowerInvariant().Trim();

            if (dto.Phone != null)
                academy.Phone = dto.Phone;

            if (dto.Address != null)
                academy.Address = Utils.SanitizeInput(dto.Address);

            academy.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            return new
            {
                academyId = academy.AcademyId,
                name = academy.Name,
                slug = academy.Slug,
                description = academy.Description,
                logoUrl = academy.LogoUrl,
                bannerUrl = academy.BannerUrl,
                email = academy.Email,
                phone = academy.Phone,
                address = academy.Address,
                ownerId = academy.OwnerId,
                isVerified = academy.IsVerified,
                isActive = academy.IsActive,
                createdAt = academy.CreatedAt,
                updatedAt = academy.UpdatedAt
            };
        }
    }
}