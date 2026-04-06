using Epecps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Epecps.Infrastructure.Migrations
{
    [DbContext(typeof(EpecpsDbContext))]
    [Migration("20260401101500_AddPersonalGoalCompletionDetails")]
    public partial class AddPersonalGoalCompletionDetails : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CompletionEvidenceUrl",
                table: "PersonalGoals",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CompletionCertificationUrl",
                table: "PersonalGoals",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CompletionSummary",
                table: "PersonalGoals",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CompletionComment",
                table: "PersonalGoals",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CompletionEvidenceUrl",
                table: "PersonalGoals");

            migrationBuilder.DropColumn(
                name: "CompletionCertificationUrl",
                table: "PersonalGoals");

            migrationBuilder.DropColumn(
                name: "CompletionSummary",
                table: "PersonalGoals");

            migrationBuilder.DropColumn(
                name: "CompletionComment",
                table: "PersonalGoals");
        }
    }
}
