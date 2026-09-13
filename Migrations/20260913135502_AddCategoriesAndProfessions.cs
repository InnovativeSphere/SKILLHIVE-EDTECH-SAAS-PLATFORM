using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SkillHive.Migrations
{
    /// <inheritdoc />
    public partial class AddCategoriesAndProfessions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CATEGORIES",
                columns: table => new
                {
                    CategoryId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Slug = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    IsGlobal = table.Column<bool>(type: "boolean", nullable: false),
                    AcademyId = table.Column<int>(type: "integer", nullable: true),
                    PreviousNames = table.Column<List<string>>(type: "text[]", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CATEGORIES", x => x.CategoryId);
                    table.ForeignKey(
                        name: "FK_CATEGORIES_ACADEMIES_AcademyId",
                        column: x => x.AcademyId,
                        principalTable: "ACADEMIES",
                        principalColumn: "AcademyId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PROFESSIONS",
                columns: table => new
                {
                    ProfessionId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Slug = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    CategoryId = table.Column<int>(type: "integer", nullable: false),
                    IsGlobal = table.Column<bool>(type: "boolean", nullable: false),
                    AcademyId = table.Column<int>(type: "integer", nullable: true),
                    PreviousNames = table.Column<List<string>>(type: "text[]", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PROFESSIONS", x => x.ProfessionId);
                    table.ForeignKey(
                        name: "FK_PROFESSIONS_ACADEMIES_AcademyId",
                        column: x => x.AcademyId,
                        principalTable: "ACADEMIES",
                        principalColumn: "AcademyId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PROFESSIONS_CATEGORIES_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "CATEGORIES",
                        principalColumn: "CategoryId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CATEGORIES_AcademyId",
                table: "CATEGORIES",
                column: "AcademyId");

            migrationBuilder.CreateIndex(
                name: "IX_CATEGORIES_Slug_AcademyId",
                table: "CATEGORIES",
                columns: new[] { "Slug", "AcademyId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PROFESSIONS_AcademyId",
                table: "PROFESSIONS",
                column: "AcademyId");

            migrationBuilder.CreateIndex(
                name: "IX_PROFESSIONS_CategoryId",
                table: "PROFESSIONS",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_PROFESSIONS_Slug_AcademyId",
                table: "PROFESSIONS",
                columns: new[] { "Slug", "AcademyId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PROFESSIONS");

            migrationBuilder.DropTable(
                name: "CATEGORIES");
        }
    }
}
