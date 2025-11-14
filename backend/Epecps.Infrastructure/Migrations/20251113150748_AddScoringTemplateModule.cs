using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Epecps.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddScoringTemplateModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ScoreTemplates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Version = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    IsPublished = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    IsArchived = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedByUserId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScoreTemplates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ScoreCategories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ScoreTemplateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    WeightPercent = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    MaxScore = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScoreCategories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ScoreCategories_ScoreTemplates_ScoreTemplateId",
                        column: x => x.ScoreTemplateId,
                        principalTable: "ScoreTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ScoreItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ScoreCategoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ItemType = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    MaxScore = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    WeightWithinCategory = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: true),
                    IsMandatory = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    EvidenceRequired = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    EvidenceHint = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScoreItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ScoreItems_ScoreCategories_ScoreCategoryId",
                        column: x => x.ScoreCategoryId,
                        principalTable: "ScoreCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ScoreCategories_IsActive",
                table: "ScoreCategories",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_ScoreCategories_ScoreTemplateId",
                table: "ScoreCategories",
                column: "ScoreTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_ScoreCategories_ScoreTemplateId_DisplayOrder",
                table: "ScoreCategories",
                columns: new[] { "ScoreTemplateId", "DisplayOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_ScoreItems_IsActive",
                table: "ScoreItems",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_ScoreItems_ScoreCategoryId",
                table: "ScoreItems",
                column: "ScoreCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_ScoreItems_ScoreCategoryId_DisplayOrder",
                table: "ScoreItems",
                columns: new[] { "ScoreCategoryId", "DisplayOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_ScoreTemplates_IsArchived",
                table: "ScoreTemplates",
                column: "IsArchived");

            migrationBuilder.CreateIndex(
                name: "IX_ScoreTemplates_IsPublished",
                table: "ScoreTemplates",
                column: "IsPublished");

            migrationBuilder.CreateIndex(
                name: "IX_ScoreTemplates_Name",
                table: "ScoreTemplates",
                column: "Name");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ScoreItems");

            migrationBuilder.DropTable(
                name: "ScoreCategories");

            migrationBuilder.DropTable(
                name: "ScoreTemplates");
        }
    }
}
