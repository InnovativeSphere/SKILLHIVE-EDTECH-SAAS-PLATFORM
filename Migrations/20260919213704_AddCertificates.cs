using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SkillHive.Migrations
{
    /// <inheritdoc />
    public partial class AddCertificates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CERTIFICATES",
                columns: table => new
                {
                    CertificateId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EnrollmentId = table.Column<int>(type: "integer", nullable: false),
                    StudentId = table.Column<int>(type: "integer", nullable: false),
                    CourseId = table.Column<int>(type: "integer", nullable: false),
                    AcademyId = table.Column<int>(type: "integer", nullable: false),
                    VerificationCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    PdfUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    CloudinaryPublicId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    IssuedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IssuedByUserId = table.Column<int>(type: "integer", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    RevokedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RevokedReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CERTIFICATES", x => x.CertificateId);
                    table.ForeignKey(
                        name: "FK_CERTIFICATES_ACADEMIES_AcademyId",
                        column: x => x.AcademyId,
                        principalTable: "ACADEMIES",
                        principalColumn: "AcademyId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CERTIFICATES_COURSES_CourseId",
                        column: x => x.CourseId,
                        principalTable: "COURSES",
                        principalColumn: "CourseId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CERTIFICATES_ENROLLMENTS_EnrollmentId",
                        column: x => x.EnrollmentId,
                        principalTable: "ENROLLMENTS",
                        principalColumn: "EnrollmentId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CERTIFICATES_USERS_IssuedByUserId",
                        column: x => x.IssuedByUserId,
                        principalTable: "USERS",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CERTIFICATES_USERS_StudentId",
                        column: x => x.StudentId,
                        principalTable: "USERS",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CERTIFICATES_AcademyId",
                table: "CERTIFICATES",
                column: "AcademyId");

            migrationBuilder.CreateIndex(
                name: "IX_CERTIFICATES_CourseId",
                table: "CERTIFICATES",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_CERTIFICATES_EnrollmentId",
                table: "CERTIFICATES",
                column: "EnrollmentId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CERTIFICATES_IssuedByUserId",
                table: "CERTIFICATES",
                column: "IssuedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_CERTIFICATES_StudentId",
                table: "CERTIFICATES",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_CERTIFICATES_VerificationCode",
                table: "CERTIFICATES",
                column: "VerificationCode",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CERTIFICATES");
        }
    }
}
