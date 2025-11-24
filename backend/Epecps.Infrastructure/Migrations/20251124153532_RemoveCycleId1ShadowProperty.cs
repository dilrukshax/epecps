using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Epecps.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveCycleId1ShadowProperty : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Evaluations_Cycles_CycleId1",
                table: "Evaluations");

            migrationBuilder.DropIndex(
                name: "IX_Evaluations_CycleId1",
                table: "Evaluations");

            migrationBuilder.DropColumn(
                name: "CycleId1",
                table: "Evaluations");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CycleId1",
                table: "Evaluations",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Evaluations_CycleId1",
                table: "Evaluations",
                column: "CycleId1");

            migrationBuilder.AddForeignKey(
                name: "FK_Evaluations_Cycles_CycleId1",
                table: "Evaluations",
                column: "CycleId1",
                principalTable: "Cycles",
                principalColumn: "CycleId");
        }
    }
}
