using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Epecps.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddGoalSetIdToPersonalGoals : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "GoalSetId",
                table: "PersonalGoals",
                type: "uniqueidentifier",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GoalSetId",
                table: "PersonalGoals");
        }
    }
}
