using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Epecps.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixUserDepartmentRelationship : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AuditLogs_Users_ActorUserId",
                table: "AuditLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_Evaluations_Users_EmployeeId",
                table: "Evaluations");

            migrationBuilder.DropForeignKey(
                name: "FK_Evaluations_Users_ReportingManagerId",
                table: "Evaluations");

            migrationBuilder.DropForeignKey(
                name: "FK_Evaluations_Users_TeamLeadId",
                table: "Evaluations");

            migrationBuilder.DropForeignKey(
                name: "FK_PeerAssignments_Users_PeerUserId",
                table: "PeerAssignments");

            migrationBuilder.DropForeignKey(
                name: "FK_PersonalGoalActivities_SuggestedActivities_SuggestedActivityId",
                table: "PersonalGoalActivities");

            migrationBuilder.DropForeignKey(
                name: "FK_PromotionCases_Users_GmDecidedById",
                table: "PromotionCases");

            migrationBuilder.DropForeignKey(
                name: "FK_PromotionCases_Users_RecommendedByHodId",
                table: "PromotionCases");

            migrationBuilder.DropForeignKey(
                name: "FK_Reviews_Users_ReviewerUserId",
                table: "Reviews");

            migrationBuilder.DropForeignKey(
                name: "FK_Users_Departments_DepartmentDeptId",
                table: "Users");

            migrationBuilder.DropTable(
                name: "SuggestedActivities");

            migrationBuilder.DropIndex(
                name: "IX_Users_DepartmentDeptId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_PersonalGoalActivities_SuggestedActivityId",
                table: "PersonalGoalActivities");

            migrationBuilder.DropColumn(
                name: "DepartmentDeptId",
                table: "Users");

            migrationBuilder.AddForeignKey(
                name: "FK_AuditLogs_Users_ActorUserId",
                table: "AuditLogs",
                column: "ActorUserId",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Evaluations_Users_EmployeeId",
                table: "Evaluations",
                column: "EmployeeId",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Evaluations_Users_ReportingManagerId",
                table: "Evaluations",
                column: "ReportingManagerId",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Evaluations_Users_TeamLeadId",
                table: "Evaluations",
                column: "TeamLeadId",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PeerAssignments_Users_PeerUserId",
                table: "PeerAssignments",
                column: "PeerUserId",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PromotionCases_Users_GmDecidedById",
                table: "PromotionCases",
                column: "GmDecidedById",
                principalTable: "Users",
                principalColumn: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_PromotionCases_Users_RecommendedByHodId",
                table: "PromotionCases",
                column: "RecommendedByHodId",
                principalTable: "Users",
                principalColumn: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Reviews_Users_ReviewerUserId",
                table: "Reviews",
                column: "ReviewerUserId",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AuditLogs_Users_ActorUserId",
                table: "AuditLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_Evaluations_Users_EmployeeId",
                table: "Evaluations");

            migrationBuilder.DropForeignKey(
                name: "FK_Evaluations_Users_ReportingManagerId",
                table: "Evaluations");

            migrationBuilder.DropForeignKey(
                name: "FK_Evaluations_Users_TeamLeadId",
                table: "Evaluations");

            migrationBuilder.DropForeignKey(
                name: "FK_PeerAssignments_Users_PeerUserId",
                table: "PeerAssignments");

            migrationBuilder.DropForeignKey(
                name: "FK_PromotionCases_Users_GmDecidedById",
                table: "PromotionCases");

            migrationBuilder.DropForeignKey(
                name: "FK_PromotionCases_Users_RecommendedByHodId",
                table: "PromotionCases");

            migrationBuilder.DropForeignKey(
                name: "FK_Reviews_Users_ReviewerUserId",
                table: "Reviews");

            migrationBuilder.AddColumn<int>(
                name: "DepartmentDeptId",
                table: "Users",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "SuggestedActivities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ScoreItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SuggestedActivities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SuggestedActivities_ScoreItems_ScoreItemId",
                        column: x => x.ScoreItemId,
                        principalTable: "ScoreItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Users_DepartmentDeptId",
                table: "Users",
                column: "DepartmentDeptId");

            migrationBuilder.CreateIndex(
                name: "IX_PersonalGoalActivities_SuggestedActivityId",
                table: "PersonalGoalActivities",
                column: "SuggestedActivityId");

            migrationBuilder.CreateIndex(
                name: "IX_SuggestedActivities_ScoreItemId",
                table: "SuggestedActivities",
                column: "ScoreItemId");

            migrationBuilder.CreateIndex(
                name: "IX_SuggestedActivities_ScoreItemId_DisplayOrder",
                table: "SuggestedActivities",
                columns: new[] { "ScoreItemId", "DisplayOrder" });

            migrationBuilder.AddForeignKey(
                name: "FK_AuditLogs_Users_ActorUserId",
                table: "AuditLogs",
                column: "ActorUserId",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Evaluations_Users_EmployeeId",
                table: "Evaluations",
                column: "EmployeeId",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Evaluations_Users_ReportingManagerId",
                table: "Evaluations",
                column: "ReportingManagerId",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Evaluations_Users_TeamLeadId",
                table: "Evaluations",
                column: "TeamLeadId",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PeerAssignments_Users_PeerUserId",
                table: "PeerAssignments",
                column: "PeerUserId",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PersonalGoalActivities_SuggestedActivities_SuggestedActivityId",
                table: "PersonalGoalActivities",
                column: "SuggestedActivityId",
                principalTable: "SuggestedActivities",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_PromotionCases_Users_GmDecidedById",
                table: "PromotionCases",
                column: "GmDecidedById",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PromotionCases_Users_RecommendedByHodId",
                table: "PromotionCases",
                column: "RecommendedByHodId",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Reviews_Users_ReviewerUserId",
                table: "Reviews",
                column: "ReviewerUserId",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Departments_DepartmentDeptId",
                table: "Users",
                column: "DepartmentDeptId",
                principalTable: "Departments",
                principalColumn: "DeptId");
        }
    }
}
