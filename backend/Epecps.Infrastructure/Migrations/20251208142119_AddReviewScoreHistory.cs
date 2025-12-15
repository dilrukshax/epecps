using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Epecps.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddReviewScoreHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ReviewScoreHistory",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReviewId = table.Column<int>(type: "int", nullable: false),
                    EvaluationId = table.Column<int>(type: "int", nullable: false),
                    ReviewerUserId = table.Column<int>(type: "int", nullable: false),
                    ReviewerRole = table.Column<int>(type: "int", nullable: false),
                    PersonalGoalId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    GoalTitle = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    PreviousScore = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    NewScore = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    PreviousComment = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    NewComment = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Action = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReviewScoreHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReviewScoreHistory_Evaluations_EvaluationId",
                        column: x => x.EvaluationId,
                        principalTable: "Evaluations",
                        principalColumn: "EvaluationId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReviewScoreHistory_PersonalGoals_PersonalGoalId",
                        column: x => x.PersonalGoalId,
                        principalTable: "PersonalGoals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ReviewScoreHistory_Reviews_ReviewId",
                        column: x => x.ReviewId,
                        principalTable: "Reviews",
                        principalColumn: "ReviewId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ReviewScoreHistory_Users_ReviewerUserId",
                        column: x => x.ReviewerUserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ReviewScoreHistory_CreatedAt",
                table: "ReviewScoreHistory",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ReviewScoreHistory_EvaluationId",
                table: "ReviewScoreHistory",
                column: "EvaluationId");

            migrationBuilder.CreateIndex(
                name: "IX_ReviewScoreHistory_PersonalGoalId",
                table: "ReviewScoreHistory",
                column: "PersonalGoalId");

            migrationBuilder.CreateIndex(
                name: "IX_ReviewScoreHistory_ReviewerUserId",
                table: "ReviewScoreHistory",
                column: "ReviewerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ReviewScoreHistory_ReviewId",
                table: "ReviewScoreHistory",
                column: "ReviewId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ReviewScoreHistory");
        }
    }
}
