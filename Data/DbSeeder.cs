using SkillHive.Data.Seed;

namespace SkillHive.Data
{
    public class DbSeeder
    {
        private readonly AppDbContext _db;
        private readonly ILogger<DbSeeder> _logger;

        public DbSeeder(AppDbContext db, ILogger<DbSeeder> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task SeedAsync()
        {
            try
            {
                await SeedUsers.SeedSuperAdminAsync(_db);
                _logger.LogInformation("Seeded superadmin user.");

                await SeedCategories.SeedGlobalTaxonomyAsync(_db);
                _logger.LogInformation("Seeded global taxonomy.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Database seeding failed.");
                throw;
            }
        }
    }
}