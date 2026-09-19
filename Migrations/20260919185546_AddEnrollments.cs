using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SkillHive.Migrations
{
    /// <inheritdoc />
    public partial class AddEnrollments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ENROLLMENTS",
                columns: table => new
                {
                    EnrollmentId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    StudentId = table.Column<int>(type: "integer", nullable: false),
                    CourseId = table.Column<int>(type: "integer", nullable: false),
                    PaymentId = table.Column<int>(type: "integer", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    EnrolledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastAccessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CertificateIssuedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ProgressPercentage = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ENROLLMENTS", x => x.EnrollmentId);
                    table.ForeignKey(
                        name: "FK_ENROLLMENTS_COURSES_CourseId",
                        column: x => x.CourseId,
                        principalTable: "COURSES",
                        principalColumn: "CourseId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ENROLLMENTS_USERS_StudentId",
                        column: x => x.StudentId,
                        principalTable: "USERS",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "STUDENT_ACADEMY_FOLLOWS",
                columns: table => new
                {
                    FollowId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    StudentId = table.Column<int>(type: "integer", nullable: false),
                    AcademyId = table.Column<int>(type: "integer", nullable: false),
                    NotifyOnNewCourse = table.Column<bool>(type: "boolean", nullable: false),
                    FollowedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_STUDENT_ACADEMY_FOLLOWS", x => x.FollowId);
                    table.ForeignKey(
                        name: "FK_STUDENT_ACADEMY_FOLLOWS_ACADEMIES_AcademyId",
                        column: x => x.AcademyId,
                        principalTable: "ACADEMIES",
                        principalColumn: "AcademyId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_STUDENT_ACADEMY_FOLLOWS_USERS_StudentId",
                        column: x => x.StudentId,
                        principalTable: "USERS",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LESSON_PROGRESS",
                columns: table => new
                {
                    ProgressId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EnrollmentId = table.Column<int>(type: "integer", nullable: false),
                    LessonId = table.Column<int>(type: "integer", nullable: false),
                    IsCompleted = table.Column<bool>(type: "boolean", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LESSON_PROGRESS", x => x.ProgressId);
                    table.ForeignKey(
                        name: "FK_LESSON_PROGRESS_ENROLLMENTS_EnrollmentId",
                        column: x => x.EnrollmentId,
                        principalTable: "ENROLLMENTS",
                        principalColumn: "EnrollmentId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LESSON_PROGRESS_LESSONS_LessonId",
                        column: x => x.LessonId,
                        principalTable: "LESSONS",
                        principalColumn: "LessonId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ENROLLMENTS_CourseId",
                table: "ENROLLMENTS",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_ENROLLMENTS_StudentId_CourseId",
                table: "ENROLLMENTS",
                columns: new[] { "StudentId", "CourseId" });

            migrationBuilder.CreateIndex(
                name: "IX_LESSON_PROGRESS_EnrollmentId_LessonId",
                table: "LESSON_PROGRESS",
                columns: new[] { "EnrollmentId", "LessonId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LESSON_PROGRESS_LessonId",
                table: "LESSON_PROGRESS",
                column: "LessonId");

            migrationBuilder.CreateIndex(
                name: "IX_STUDENT_ACADEMY_FOLLOWS_AcademyId",
                table: "STUDENT_ACADEMY_FOLLOWS",
                column: "AcademyId");

            migrationBuilder.CreateIndex(
                name: "IX_STUDENT_ACADEMY_FOLLOWS_StudentId_AcademyId",
                table: "STUDENT_ACADEMY_FOLLOWS",
                columns: new[] { "StudentId", "AcademyId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LESSON_PROGRESS");

            migrationBuilder.DropTable(
                name: "STUDENT_ACADEMY_FOLLOWS");

            migrationBuilder.DropTable(
                name: "ENROLLMENTS");
        }
    }
}
