using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EduCore.Migrations
{
    /// <inheritdoc />
    public partial class AddFeeGradeLevelLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "GradeLevelId",
                table: "Fees",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Fees_GradeLevelId",
                table: "Fees",
                column: "GradeLevelId");

            migrationBuilder.AddForeignKey(
                name: "FK_Fees_GradeLevels_GradeLevelId",
                table: "Fees",
                column: "GradeLevelId",
                principalTable: "GradeLevels",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Fees_GradeLevels_GradeLevelId",
                table: "Fees");

            migrationBuilder.DropIndex(
                name: "IX_Fees_GradeLevelId",
                table: "Fees");

            migrationBuilder.DropColumn(
                name: "GradeLevelId",
                table: "Fees");
        }
    }
}
