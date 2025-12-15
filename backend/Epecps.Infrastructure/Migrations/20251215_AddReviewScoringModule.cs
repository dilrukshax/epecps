using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Epecps.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddReviewScoringModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add OverallScore column to Reviews table
            migrationBuilder.AddColumn<decimal>(
                name: "OverallScore",
                table: "Reviews",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: true);

            // Create ReviewScores table
            migrationBuilder.CreateTable(
                name: "ReviewScores",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EvaluationId = table.Column<int>(type: "int", nullable: false),
                    ReviewId = table.Column<int>(type: "int", nullable: false),
                    ReviewerId = table.Column<int>(type: "int", nullable: false),
                    PersonalGoalId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ScoreValue = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    Comment = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReviewScores", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReviewScores_Evaluations_EvaluationId",
                        column: x => x.EvaluationId,
                        principalTable: "Evaluations",
                        principalColumn: "EvaluationId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ReviewScores_PersonalGoals_PersonalGoalId",
                        column: x => x.PersonalGoalId,
                        principalTable: "PersonalGoals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ReviewScores_Reviews_ReviewId",
                        column: x => x.ReviewId,
                        principalTable: "Reviews",
                        principalColumn: "ReviewId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ReviewScores_Users_ReviewerId",
                        column: x => x.ReviewerId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            // Create indexes
            migrationBuilder.CreateIndex(
                name: "IX_ReviewScores_EvaluationId",
                table: "ReviewScores",
                column: "EvaluationId");

            migrationBuilder.CreateIndex(
                name: "IX_ReviewScores_EvaluationId_ReviewerId",
                table: "ReviewScores",
                columns: new[] { "EvaluationId", "ReviewerId" });

            migrationBuilder.CreateIndex(
                name: "IX_ReviewScores_PersonalGoalId",
                table: "ReviewScores",
                column: "PersonalGoalId");

            migrationBuilder.CreateIndex(
                name: "IX_ReviewScores_ReviewerId",
                table: "ReviewScores",
                column: "ReviewerId");

            migrationBuilder.CreateIndex(
                name: "IX_ReviewScores_ReviewId",
                table: "ReviewScores",
                column: "ReviewId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop ReviewScores table
            migrationBuilder.DropTable(
                name: "ReviewScores");

            // Remove OverallScore column from Reviews table
            migrationBuilder.DropColumn(
                name: "OverallScore",
                table: "Reviews");
        }
    }
}
