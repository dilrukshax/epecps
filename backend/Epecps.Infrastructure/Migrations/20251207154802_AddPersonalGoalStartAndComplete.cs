using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Epecps.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPersonalGoalStartAndComplete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CompletedAt",
                table: "PersonalGoals",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "StartedAt",
                table: "PersonalGoals",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PersonalGoals_GoalSetId",
                table: "PersonalGoals",
                column: "GoalSetId");

            migrationBuilder.CreateIndex(
                name: "IX_PersonalGoals_UserId_GoalSetId",
                table: "PersonalGoals",
                columns: new[] { "UserId", "GoalSetId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PersonalGoals_GoalSetId",
                table: "PersonalGoals");

            migrationBuilder.DropIndex(
                name: "IX_PersonalGoals_UserId_GoalSetId",
                table: "PersonalGoals");

            migrationBuilder.DropColumn(
                name: "CompletedAt",
                table: "PersonalGoals");

            migrationBuilder.DropColumn(
                name: "StartedAt",
                table: "PersonalGoals");
        }
    }
}
