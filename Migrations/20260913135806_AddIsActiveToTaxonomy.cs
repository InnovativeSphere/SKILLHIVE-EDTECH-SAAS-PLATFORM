using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SkillHive.Migrations
{
    /// <inheritdoc />
    public partial class AddIsActiveToTaxonomy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "PROFESSIONS",
                type: "boolean",
                nullable: false,
               defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "CATEGORIES",
                type: "boolean",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "PROFESSIONS");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "CATEGORIES");
        }
    }
}
