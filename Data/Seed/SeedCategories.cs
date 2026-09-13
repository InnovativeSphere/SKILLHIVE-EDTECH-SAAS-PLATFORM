using Microsoft.EntityFrameworkCore;
using SkillHive.Common;
using SkillHive.Models;

namespace SkillHive.Data.Seed
{
    public static class SeedCategories
    {
        public static async Task SeedGlobalTaxonomyAsync(AppDbContext db)
        {
            // Only seed if no global categories exist yet
            if (await db.Categories.AnyAsync(c => c.IsGlobal))
                return;

            var taxonomy = new Dictionary<string, string[]>
            {
                { "Tech", new[] { "Frontend Developer", "Backend Developer", "Mobile Developer", "DevOps Engineer", "Data Analyst", "Data Scientist", "UI/UX Designer", "Cybersecurity Analyst" } },
                { "Culinary", new[] { "Baking", "Pastry", "Cooking", "Catering", "Mixology", "Food Photography" } },
                { "Design", new[] { "Graphic Design", "Interior Design", "Fashion Design", "Product Design", "Motion Graphics", "Illustration" } },
                { "Construction", new[] { "Masonry", "Carpentry", "Electrical Installation", "Plumbing", "Welding", "Painting" } },
                { "Business", new[] { "Digital Marketing", "Project Management", "Sales", "Entrepreneurship", "Accounting", "Customer Service" } },
                { "Languages", new[] { "English", "French", "Arabic", "Hausa", "Yoruba", "Igbo", "Mandarin", "Spanish" } },
                { "Arts", new[] { "Photography", "Videography", "Music Production", "Writing", "Acting", "Drawing" } },
                { "Health", new[] { "Fitness Training", "Nutrition", "First Aid", "Yoga", "Massage Therapy" } }
            };

            foreach (var (categoryName, professionNames) in taxonomy)
            {
                var categorySlug = SlugHelper.GenerateSlug(categoryName);

                var category = new Category
                {
                    Name = categoryName,
                    Slug = categorySlug,
                    IsGlobal = true,
                    AcademyId = null,
                    IsActive = true,
                    PreviousNames = new List<string>(),
                    CreatedAt = DateTime.UtcNow
                };

                db.Categories.Add(category);
                await db.SaveChangesAsync();

                foreach (var professionName in professionNames)
                {
                    db.Professions.Add(new Profession
                    {
                        Name = professionName,
                        Slug = SlugHelper.GenerateSlug(professionName),
                        CategoryId = category.CategoryId,
                        IsGlobal = true,
                        AcademyId = null,
                        IsActive = true,
                        PreviousNames = new List<string>(),
                        CreatedAt = DateTime.UtcNow
                    });
                }

                await db.SaveChangesAsync();
            }
        }
    }
}