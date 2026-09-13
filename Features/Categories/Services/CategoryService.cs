using Microsoft.EntityFrameworkCore;
using SkillHive.Common;
using SkillHive.Data;
using SkillHive.Enums;
using SkillHive.Features.Categories.DTOs;
using SkillHive.Models;

namespace SkillHive.Features.Categories.Services
{
    public class CategoryService
    {
        private readonly AppDbContext _db;

        public CategoryService(AppDbContext db)
        {
            _db = db;
        }

        // ─── List Categories (public) ──────────────────────────────
        // Global categories + academy-specific categories for a given academy slug
        public async Task<List<object>> GetCategoriesAsync(string? academySlug = null)
        {
            int? academyId = null;

            if (!string.IsNullOrWhiteSpace(academySlug))
            {
                var academy = await _db.Academies.FirstOrDefaultAsync(a => a.Slug == academySlug);
                academyId = academy?.AcademyId;
            }

            var categories = await _db.Categories
                .Where(c => c.IsActive
                            && (c.IsGlobal || (academyId.HasValue && c.AcademyId == academyId)))
                .OrderBy(c => c.Name)
                .Select(c => new
                {
                    categoryId = c.CategoryId,
                    name = c.Name,
                    slug = c.Slug,
                    isGlobal = c.IsGlobal,
                    academyId = c.AcademyId,
                    professions = _db.Professions
                        .Where(p => p.CategoryId == c.CategoryId && p.IsActive)
                        .OrderBy(p => p.Name)
                        .Select(p => new
                        {
                            professionId = p.ProfessionId,
                            name = p.Name,
                            slug = p.Slug,
                            isGlobal = p.IsGlobal,
                            academyId = p.AcademyId
                        })
                        .ToList()
                })
                .ToListAsync();

            return categories.Cast<object>().ToList();
        }

        // ─── Create Category (Superadmin = global, Owner = academy-scoped) ──
        public async Task<object> CreateCategoryAsync(int userId, UserRole role, int? academyId, CreateCategoryDto dto)
        {
            var name = Utils.ToTitleCase(Utils.SanitizeInput(dto.Name));
            var baseSlug = SlugHelper.GenerateSlug(name);

            bool isGlobal = role == UserRole.SUPER_ADMIN && dto.IsGlobal;
            int? targetAcademyId = isGlobal ? null : academyId;

            if (!isGlobal && targetAcademyId == null)
                throw new InvalidOperationException("Only superadmins can create global categories. Academy-scoped users must have an academy.");

            // Uniqueness: no duplicate name at the same scope
            var duplicate = await _db.Categories
                .AnyAsync(c => c.Name.ToLower() == name.ToLower()
                               && c.AcademyId == targetAcademyId);

            if (duplicate)
                throw new InvalidOperationException("A category with this name already exists at this scope");

            // Also prevent an academy from duplicating a global name
            if (!isGlobal && targetAcademyId != null)
            {
                var globalDuplicate = await _db.Categories
                    .AnyAsync(c => c.Name.ToLower() == name.ToLower() && c.IsGlobal);

                if (globalDuplicate)
                    throw new InvalidOperationException("This name is already used by a global category");
            }

            var slug = await SlugHelper.EnsureUniqueSlugAsync(
                baseSlug,
                async s => await _db.Categories.AnyAsync(c => c.Slug == s && c.AcademyId == targetAcademyId));

            var category = new Category
            {
                Name = name,
                Slug = slug,
                IsGlobal = isGlobal,
                AcademyId = targetAcademyId,
                IsActive = true,
                PreviousNames = new List<string>(),
                CreatedAt = DateTime.UtcNow
            };

            _db.Categories.Add(category);
            await _db.SaveChangesAsync();

            return new
            {
                categoryId = category.CategoryId,
                name = category.Name,
                slug = category.Slug,
                isGlobal = category.IsGlobal,
                academyId = category.AcademyId,
                isActive = category.IsActive,
                createdAt = category.CreatedAt
            };
        }

        // ─── Update Category (with previousNames tracking) ────────
        public async Task<object> UpdateCategoryAsync(int userId, UserRole role, int? academyId, int categoryId, UpdateCategoryDto dto)
        {
            var category = await _db.Categories.FirstOrDefaultAsync(c => c.CategoryId == categoryId);
            if (category == null)
                throw new InvalidOperationException("Category not found");

            EnsureCanModify(role, academyId, category.IsGlobal, category.AcademyId);

            if (!string.IsNullOrWhiteSpace(dto.Name))
            {
                var newName = Utils.ToTitleCase(Utils.SanitizeInput(dto.Name));

                if (!string.Equals(newName, category.Name, StringComparison.OrdinalIgnoreCase))
                {
                    // Uniqueness at the same scope, excluding self
                    var duplicate = await _db.Categories
                        .AnyAsync(c => c.CategoryId != categoryId
                                       && c.Name.ToLower() == newName.ToLower()
                                       && c.AcademyId == category.AcademyId);

                    if (duplicate)
                        throw new InvalidOperationException("Another category with this name exists at this scope");

                    category.PreviousNames.Add(category.Name);
                    category.Name = newName;
                }
            }

            await _db.SaveChangesAsync();

            return new
            {
                categoryId = category.CategoryId,
                name = category.Name,
                slug = category.Slug,
                isGlobal = category.IsGlobal,
                academyId = category.AcademyId,
                previousNames = category.PreviousNames,
                isActive = category.IsActive
            };
        }

        // ─── Deactivate Category ───────────────────────────────────
        public async Task<object> DeactivateCategoryAsync(int userId, UserRole role, int? academyId, int categoryId)
        {
            var category = await _db.Categories.FirstOrDefaultAsync(c => c.CategoryId == categoryId);
            if (category == null)
                throw new InvalidOperationException("Category not found");

            EnsureCanModify(role, academyId, category.IsGlobal, category.AcademyId);

            // Block if any active profession references this category
            var hasActiveProfessions = await _db.Professions
                .AnyAsync(p => p.CategoryId == categoryId && p.IsActive);

            if (hasActiveProfessions)
                throw new InvalidOperationException("Cannot deactivate a category with active professions");

            category.IsActive = false;
            await _db.SaveChangesAsync();

            return new { categoryId = category.CategoryId, isActive = false, message = "Category deactivated" };
        }

        // ─── List Professions (filtered by category optional) ─────
        public async Task<List<object>> GetProfessionsAsync(string? academySlug, int? categoryId = null)
        {
            int? academyId = null;

            if (!string.IsNullOrWhiteSpace(academySlug))
            {
                var academy = await _db.Academies.FirstOrDefaultAsync(a => a.Slug == academySlug);
                academyId = academy?.AcademyId;
            }

            var query = _db.Professions
                .Where(p => p.IsActive
                            && (p.IsGlobal || (academyId.HasValue && p.AcademyId == academyId)));

            if (categoryId.HasValue)
                query = query.Where(p => p.CategoryId == categoryId.Value);

            var professions = await query
                .OrderBy(p => p.Name)
                .Select(p => new
                {
                    professionId = p.ProfessionId,
                    name = p.Name,
                    slug = p.Slug,
                    categoryId = p.CategoryId,
                    isGlobal = p.IsGlobal,
                    academyId = p.AcademyId
                })
                .ToListAsync();

            return professions.Cast<object>().ToList();
        }

        // ─── Create Profession ─────────────────────────────────────
        public async Task<object> CreateProfessionAsync(int userId, UserRole role, int? academyId, CreateProfessionDto dto)
        {
            var name = Utils.ToTitleCase(Utils.SanitizeInput(dto.Name));
            var baseSlug = SlugHelper.GenerateSlug(name);

            var category = await _db.Categories.FirstOrDefaultAsync(c => c.CategoryId == dto.CategoryId && c.IsActive);
            if (category == null)
                throw new InvalidOperationException("Category not found or inactive");

            bool isGlobal = role == UserRole.SUPER_ADMIN && dto.IsGlobal;
            int? targetAcademyId = isGlobal ? null : academyId;

            if (!isGlobal && targetAcademyId == null)
                throw new InvalidOperationException("Only superadmins can create global professions. Academy users must have an academy.");

            // Academy users can only add professions under global categories
            if (!isGlobal && !category.IsGlobal)
                throw new InvalidOperationException("You can only add professions under a global category");

            var duplicate = await _db.Professions
                .AnyAsync(p => p.CategoryId == dto.CategoryId
                               && p.Name.ToLower() == name.ToLower()
                               && p.AcademyId == targetAcademyId);

            if (duplicate)
                throw new InvalidOperationException("A profession with this name already exists in this category at this scope");

            var slug = await SlugHelper.EnsureUniqueSlugAsync(
                baseSlug,
                async s => await _db.Professions.AnyAsync(p => p.Slug == s && p.AcademyId == targetAcademyId));

            var profession = new Profession
            {
                Name = name,
                Slug = slug,
                CategoryId = dto.CategoryId,
                IsGlobal = isGlobal,
                AcademyId = targetAcademyId,
                IsActive = true,
                PreviousNames = new List<string>(),
                CreatedAt = DateTime.UtcNow
            };

            _db.Professions.Add(profession);
            await _db.SaveChangesAsync();

            return new
            {
                professionId = profession.ProfessionId,
                name = profession.Name,
                slug = profession.Slug,
                categoryId = profession.CategoryId,
                isGlobal = profession.IsGlobal,
                academyId = profession.AcademyId,
                isActive = profession.IsActive
            };
        }

        // ─── Update Profession ────────────────────────────────────
        public async Task<object> UpdateProfessionAsync(int userId, UserRole role, int? academyId, int professionId, UpdateProfessionDto dto)
        {
            var profession = await _db.Professions.FirstOrDefaultAsync(p => p.ProfessionId == professionId);
            if (profession == null)
                throw new InvalidOperationException("Profession not found");

            EnsureCanModify(role, academyId, profession.IsGlobal, profession.AcademyId);

            if (!string.IsNullOrWhiteSpace(dto.Name))
            {
                var newName = Utils.ToTitleCase(Utils.SanitizeInput(dto.Name));

                if (!string.Equals(newName, profession.Name, StringComparison.OrdinalIgnoreCase))
                {
                    var duplicate = await _db.Professions
                        .AnyAsync(p => p.ProfessionId != professionId
                                       && p.CategoryId == profession.CategoryId
                                       && p.Name.ToLower() == newName.ToLower()
                                       && p.AcademyId == profession.AcademyId);

                    if (duplicate)
                        throw new InvalidOperationException("Another profession with this name exists in this category at this scope");

                    profession.PreviousNames.Add(profession.Name);
                    profession.Name = newName;
                }
            }

            await _db.SaveChangesAsync();

            return new
            {
                professionId = profession.ProfessionId,
                name = profession.Name,
                slug = profession.Slug,
                categoryId = profession.CategoryId,
                isGlobal = profession.IsGlobal,
                academyId = profession.AcademyId,
                previousNames = profession.PreviousNames,
                isActive = profession.IsActive
            };
        }

        // ─── Deactivate Profession ────────────────────────────────
        public async Task<object> DeactivateProfessionAsync(int userId, UserRole role, int? academyId, int professionId)
        {
            var profession = await _db.Professions.FirstOrDefaultAsync(p => p.ProfessionId == professionId);
            if (profession == null)
                throw new InvalidOperationException("Profession not found");

            EnsureCanModify(role, academyId, profession.IsGlobal, profession.AcademyId);

            profession.IsActive = false;
            await _db.SaveChangesAsync();

            return new { professionId = profession.ProfessionId, isActive = false, message = "Profession deactivated" };
        }

        // ─── Permission Helper ────────────────────────────────────
        private static void EnsureCanModify(UserRole role, int? requesterAcademyId, bool isGlobal, int? ownerAcademyId)
        {
            if (role == UserRole.SUPER_ADMIN)
                return;

            if (role != UserRole.ACADEMY_OWNER)
                throw new UnauthorizedAccessException("Only academy owners and superadmins can modify taxonomy");

            if (isGlobal)
                throw new UnauthorizedAccessException("Only superadmins can modify global taxonomy");

            if (requesterAcademyId == null || requesterAcademyId != ownerAcademyId)
                throw new UnauthorizedAccessException("You can only modify taxonomy belonging to your academy");
        }
    }
}