using System;
using Epecps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Epecps.Infrastructure.Migrations
{
    [DbContext(typeof(EpecpsDbContext))]
    [Migration("20260330120000_AddWorkflowV2Core")]
    public partial class AddWorkflowV2Core : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ActivationMethod",
                table: "GoalAssignments",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ActivationReviewedAt",
                table: "GoalAssignments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ActivationReviewedByUserId",
                table: "GoalAssignments",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ActivationStatus",
                table: "GoalAssignments",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "PendingEmployee");

            migrationBuilder.AddColumn<DateTime>(
                name: "ActivationSubmittedAt",
                table: "GoalAssignments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ActivationTlComment",
                table: "GoalAssignments",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WorkflowVersion",
                table: "Evaluations",
                type: "nvarchar(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "v1");

            migrationBuilder.CreateTable(
                name: "DepartmentHodMappings",
                columns: table => new
                {
                    DeptId = table.Column<int>(type: "int", nullable: false),
                    HodUserId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DepartmentHodMappings", x => new { x.DeptId, x.HodUserId });
                    table.ForeignKey(
                        name: "FK_DepartmentHodMappings_Departments_DeptId",
                        column: x => x.DeptId,
                        principalTable: "Departments",
                        principalColumn: "DeptId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DepartmentHodMappings_Users_HodUserId",
                        column: x => x.HodUserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UserManagerMappings",
                columns: table => new
                {
                    EmployeeUserId = table.Column<int>(type: "int", nullable: false),
                    ManagerUserId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserManagerMappings", x => new { x.EmployeeUserId, x.ManagerUserId });
                    table.ForeignKey(
                        name: "FK_UserManagerMappings_Users_EmployeeUserId",
                        column: x => x.EmployeeUserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserManagerMappings_Users_ManagerUserId",
                        column: x => x.ManagerUserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WorkflowReviewWeights",
                columns: table => new
                {
                    WorkflowReviewWeightId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReviewerKey = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    WeightPercent = table.Column<decimal>(type: "decimal(6,2)", precision: 6, scale: 2, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowReviewWeights", x => x.WorkflowReviewWeightId);
                });

            migrationBuilder.CreateTable(
                name: "PipCases",
                columns: table => new
                {
                    PipCaseId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EvaluationId = table.Column<int>(type: "int", nullable: false),
                    EmployeeUserId = table.Column<int>(type: "int", nullable: false),
                    AssignedHrUserId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false, defaultValue: "Open"),
                    Reason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DueDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ClosedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PipCases", x => x.PipCaseId);
                    table.ForeignKey(
                        name: "FK_PipCases_Evaluations_EvaluationId",
                        column: x => x.EvaluationId,
                        principalTable: "Evaluations",
                        principalColumn: "EvaluationId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PipCases_Users_AssignedHrUserId",
                        column: x => x.AssignedHrUserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PipCases_Users_EmployeeUserId",
                        column: x => x.EmployeeUserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PipActionItems",
                columns: table => new
                {
                    PipActionItemId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PipCaseId = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(3000)", maxLength: 3000, nullable: true),
                    TrainingMaterialId = table.Column<int>(type: "int", nullable: true),
                    ExternalTrainingLink = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    DueDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false, defaultValue: "Pending"),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PipActionItems", x => x.PipActionItemId);
                    table.ForeignKey(
                        name: "FK_PipActionItems_PipCases_PipCaseId",
                        column: x => x.PipCaseId,
                        principalTable: "PipCases",
                        principalColumn: "PipCaseId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PipActionItems_TrainingMaterials_TrainingMaterialId",
                        column: x => x.TrainingMaterialId,
                        principalTable: "TrainingMaterials",
                        principalColumn: "TrainingMaterialId",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Evaluations_WorkflowVersion",
                table: "Evaluations",
                column: "WorkflowVersion");

            migrationBuilder.CreateIndex(
                name: "IX_GoalAssignments_ActivationReviewedByUserId",
                table: "GoalAssignments",
                column: "ActivationReviewedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_GoalAssignments_GoalSetId_ActivationStatus",
                table: "GoalAssignments",
                columns: new[] { "GoalSetId", "ActivationStatus" });

            migrationBuilder.CreateIndex(
                name: "IX_DepartmentHodMappings_HodUserId",
                table: "DepartmentHodMappings",
                column: "HodUserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserManagerMappings_ManagerUserId",
                table: "UserManagerMappings",
                column: "ManagerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowReviewWeights_ReviewerKey",
                table: "WorkflowReviewWeights",
                column: "ReviewerKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PipCases_AssignedHrUserId",
                table: "PipCases",
                column: "AssignedHrUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PipCases_EmployeeUserId",
                table: "PipCases",
                column: "EmployeeUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PipCases_EvaluationId",
                table: "PipCases",
                column: "EvaluationId");

            migrationBuilder.CreateIndex(
                name: "IX_PipCases_Status",
                table: "PipCases",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_PipActionItems_DueDate",
                table: "PipActionItems",
                column: "DueDate");

            migrationBuilder.CreateIndex(
                name: "IX_PipActionItems_PipCaseId",
                table: "PipActionItems",
                column: "PipCaseId");

            migrationBuilder.CreateIndex(
                name: "IX_PipActionItems_Status",
                table: "PipActionItems",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_PipActionItems_TrainingMaterialId",
                table: "PipActionItems",
                column: "TrainingMaterialId");

            migrationBuilder.AddForeignKey(
                name: "FK_GoalAssignments_Users_ActivationReviewedByUserId",
                table: "GoalAssignments",
                column: "ActivationReviewedByUserId",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.NoAction);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_GoalAssignments_Users_ActivationReviewedByUserId",
                table: "GoalAssignments");

            migrationBuilder.DropTable(
                name: "DepartmentHodMappings");

            migrationBuilder.DropTable(
                name: "PipActionItems");

            migrationBuilder.DropTable(
                name: "UserManagerMappings");

            migrationBuilder.DropTable(
                name: "WorkflowReviewWeights");

            migrationBuilder.DropTable(
                name: "PipCases");

            migrationBuilder.DropIndex(
                name: "IX_Evaluations_WorkflowVersion",
                table: "Evaluations");

            migrationBuilder.DropIndex(
                name: "IX_GoalAssignments_ActivationReviewedByUserId",
                table: "GoalAssignments");

            migrationBuilder.DropIndex(
                name: "IX_GoalAssignments_GoalSetId_ActivationStatus",
                table: "GoalAssignments");

            migrationBuilder.DropColumn(
                name: "ActivationMethod",
                table: "GoalAssignments");

            migrationBuilder.DropColumn(
                name: "ActivationReviewedAt",
                table: "GoalAssignments");

            migrationBuilder.DropColumn(
                name: "ActivationReviewedByUserId",
                table: "GoalAssignments");

            migrationBuilder.DropColumn(
                name: "ActivationStatus",
                table: "GoalAssignments");

            migrationBuilder.DropColumn(
                name: "ActivationSubmittedAt",
                table: "GoalAssignments");

            migrationBuilder.DropColumn(
                name: "ActivationTlComment",
                table: "GoalAssignments");

            migrationBuilder.DropColumn(
                name: "WorkflowVersion",
                table: "Evaluations");
        }
    }
}
