using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SkillHive.Migrations
{
    /// <inheritdoc />
    public partial class PartialUniqueIndexOnLessons : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_LESSONS_CourseId_Order",
                table: "LESSONS");

            migrationBuilder.CreateIndex(
                name: "IX_LESSONS_CourseId_Order",
                table: "LESSONS",
                columns: new[] { "CourseId", "Order" },
                unique: true,
                filter: "\"IsActive\" = true");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_LESSONS_CourseId_Order",
                table: "LESSONS");

            migrationBuilder.CreateIndex(
                name: "IX_LESSONS_CourseId_Order",
                table: "LESSONS",
                columns: new[] { "CourseId", "Order" },
                unique: true);
        }
    }
}
