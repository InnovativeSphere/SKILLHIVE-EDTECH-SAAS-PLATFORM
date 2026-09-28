using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SkillHive.Migrations
{
    /// <inheritdoc />
    public partial class AddSubscriptionsAndPlans : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PLANS",
                columns: table => new
                {
                    PlanId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Slug = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Price = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    Interval = table.Column<int>(type: "integer", nullable: false),
                    MaxCourses = table.Column<int>(type: "integer", nullable: true),
                    MaxStaff = table.Column<int>(type: "integer", nullable: true),
                    MaxStudentsPerCourse = table.Column<int>(type: "integer", nullable: true),
                    CanChargeCourses = table.Column<bool>(type: "boolean", nullable: false),
                    CanUseCertificates = table.Column<bool>(type: "boolean", nullable: false),
                    CanUseCustomBranding = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsPublic = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PLANS", x => x.PlanId);
                });

            migrationBuilder.CreateTable(
                name: "SUBSCRIPTIONS",
                columns: table => new
                {
                    SubscriptionId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AcademyId = table.Column<int>(type: "integer", nullable: false),
                    PlanId = table.Column<int>(type: "integer", nullable: false),
                    StartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    AutoRenew = table.Column<bool>(type: "boolean", nullable: false),
                    GraceUntil = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CancelledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SUBSCRIPTIONS", x => x.SubscriptionId);
                    table.ForeignKey(
                        name: "FK_SUBSCRIPTIONS_ACADEMIES_AcademyId",
                        column: x => x.AcademyId,
                        principalTable: "ACADEMIES",
                        principalColumn: "AcademyId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SUBSCRIPTIONS_PLANS_PlanId",
                        column: x => x.PlanId,
                        principalTable: "PLANS",
                        principalColumn: "PlanId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PLANS_Slug",
                table: "PLANS",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SUBSCRIPTIONS_AcademyId",
                table: "SUBSCRIPTIONS",
                column: "AcademyId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SUBSCRIPTIONS_PlanId",
                table: "SUBSCRIPTIONS",
                column: "PlanId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SUBSCRIPTIONS");

            migrationBuilder.DropTable(
                name: "PLANS");
        }
    }
}
