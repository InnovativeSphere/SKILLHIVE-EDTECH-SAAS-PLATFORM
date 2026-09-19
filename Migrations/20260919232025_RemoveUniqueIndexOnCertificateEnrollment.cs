using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SkillHive.Migrations
{
    /// <inheritdoc />
    public partial class RemoveUniqueIndexOnCertificateEnrollment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CERTIFICATES_VerificationCode",
                table: "CERTIFICATES");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_CERTIFICATES_VerificationCode",
                table: "CERTIFICATES",
                column: "VerificationCode",
                unique: true);
        }
    }
}
