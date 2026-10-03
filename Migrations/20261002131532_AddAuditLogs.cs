using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SkillHive.Migrations
{
    /// <inheritdoc />
    public partial class AddAuditLogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AUDIT_LOGS",
                columns: table => new
                {
                    AuditId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    AcademyId = table.Column<int>(type: "integer", nullable: true),
                    Action = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    TargetType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    TargetId = table.Column<int>(type: "integer", nullable: true),
                    Metadata = table.Column<string>(type: "text", nullable: true),
                    IpAddress = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AUDIT_LOGS", x => x.AuditId);
                    table.ForeignKey(
                        name: "FK_AUDIT_LOGS_ACADEMIES_AcademyId",
                        column: x => x.AcademyId,
                        principalTable: "ACADEMIES",
                        principalColumn: "AcademyId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AUDIT_LOGS_USERS_UserId",
                        column: x => x.UserId,
                        principalTable: "USERS",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AUDIT_LOGS_AcademyId",
                table: "AUDIT_LOGS",
                column: "AcademyId");

            migrationBuilder.CreateIndex(
                name: "IX_AUDIT_LOGS_Action",
                table: "AUDIT_LOGS",
                column: "Action");

            migrationBuilder.CreateIndex(
                name: "IX_AUDIT_LOGS_CreatedAt",
                table: "AUDIT_LOGS",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_AUDIT_LOGS_UserId",
                table: "AUDIT_LOGS",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AUDIT_LOGS");
        }
    }
}
