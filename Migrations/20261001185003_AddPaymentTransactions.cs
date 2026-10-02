using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SkillHive.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentTransactions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PAYMENT_TRANSACTIONS",
                columns: table => new
                {
                    PaymentId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Reference = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Provider = table.Column<int>(type: "integer", nullable: false),
                    Channel = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    PaidAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Purpose = table.Column<int>(type: "integer", nullable: false),
                    AcademyId = table.Column<int>(type: "integer", nullable: true),
                    InvoiceId = table.Column<int>(type: "integer", nullable: true),
                    StudentId = table.Column<int>(type: "integer", nullable: true),
                    CourseId = table.Column<int>(type: "integer", nullable: true),
                    ProviderPayload = table.Column<string>(type: "text", nullable: true),
                    FailureReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PAYMENT_TRANSACTIONS", x => x.PaymentId);
                    table.ForeignKey(
                        name: "FK_PAYMENT_TRANSACTIONS_ACADEMIES_AcademyId",
                        column: x => x.AcademyId,
                        principalTable: "ACADEMIES",
                        principalColumn: "AcademyId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PAYMENT_TRANSACTIONS_COURSES_CourseId",
                        column: x => x.CourseId,
                        principalTable: "COURSES",
                        principalColumn: "CourseId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PAYMENT_TRANSACTIONS_INVOICES_InvoiceId",
                        column: x => x.InvoiceId,
                        principalTable: "INVOICES",
                        principalColumn: "InvoiceId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PAYMENT_TRANSACTIONS_USERS_StudentId",
                        column: x => x.StudentId,
                        principalTable: "USERS",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PAYMENT_TRANSACTIONS_AcademyId",
                table: "PAYMENT_TRANSACTIONS",
                column: "AcademyId");

            migrationBuilder.CreateIndex(
                name: "IX_PAYMENT_TRANSACTIONS_CourseId",
                table: "PAYMENT_TRANSACTIONS",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_PAYMENT_TRANSACTIONS_InvoiceId",
                table: "PAYMENT_TRANSACTIONS",
                column: "InvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_PAYMENT_TRANSACTIONS_Purpose",
                table: "PAYMENT_TRANSACTIONS",
                column: "Purpose");

            migrationBuilder.CreateIndex(
                name: "IX_PAYMENT_TRANSACTIONS_Reference",
                table: "PAYMENT_TRANSACTIONS",
                column: "Reference",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PAYMENT_TRANSACTIONS_Status",
                table: "PAYMENT_TRANSACTIONS",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_PAYMENT_TRANSACTIONS_StudentId",
                table: "PAYMENT_TRANSACTIONS",
                column: "StudentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PAYMENT_TRANSACTIONS");
        }
    }
}
