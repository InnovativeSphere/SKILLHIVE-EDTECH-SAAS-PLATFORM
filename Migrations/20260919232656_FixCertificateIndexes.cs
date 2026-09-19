using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SkillHive.Migrations
{
    /// <inheritdoc />
    public partial class FixCertificateIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CERTIFICATES_EnrollmentId",
                table: "CERTIFICATES");

            migrationBuilder.CreateIndex(
                name: "IX_CERTIFICATES_EnrollmentId",
                table: "CERTIFICATES",
                column: "EnrollmentId");

            migrationBuilder.CreateIndex(
                name: "IX_CERTIFICATES_VerificationCode",
                table: "CERTIFICATES",
                column: "VerificationCode",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CERTIFICATES_EnrollmentId",
                table: "CERTIFICATES");

            migrationBuilder.DropIndex(
                name: "IX_CERTIFICATES_VerificationCode",
                table: "CERTIFICATES");

            migrationBuilder.CreateIndex(
                name: "IX_CERTIFICATES_EnrollmentId",
                table: "CERTIFICATES",
                column: "EnrollmentId",
                unique: true);
        }
    }
}
