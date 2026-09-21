using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EduCore.Migrations
{
    /// <inheritdoc />
    public partial class AddGradeLevelTuitionAmount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "Amount",
                table: "GradeLevels",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.UpdateData(
                table: "GradeLevels",
                keyColumn: "Id",
                keyValue: 1,
                column: "Amount",
                value: 11500m);

            migrationBuilder.UpdateData(
                table: "GradeLevels",
                keyColumn: "Id",
                keyValue: 2,
                column: "Amount",
                value: 13000m);

            migrationBuilder.UpdateData(
                table: "GradeLevels",
                keyColumn: "Id",
                keyValue: 3,
                column: "Amount",
                value: 14500m);

            migrationBuilder.UpdateData(
                table: "GradeLevels",
                keyColumn: "Id",
                keyValue: 4,
                column: "Amount",
                value: 16000m);

            migrationBuilder.UpdateData(
                table: "GradeLevels",
                keyColumn: "Id",
                keyValue: 5,
                column: "Amount",
                value: 17500m);

            migrationBuilder.UpdateData(
                table: "GradeLevels",
                keyColumn: "Id",
                keyValue: 6,
                column: "Amount",
                value: 19000m);

            migrationBuilder.UpdateData(
                table: "GradeLevels",
                keyColumn: "Id",
                keyValue: 7,
                column: "Amount",
                value: 20500m);

            migrationBuilder.UpdateData(
                table: "GradeLevels",
                keyColumn: "Id",
                keyValue: 8,
                column: "Amount",
                value: 22000m);

            migrationBuilder.UpdateData(
                table: "GradeLevels",
                keyColumn: "Id",
                keyValue: 9,
                column: "Amount",
                value: 23500m);

            migrationBuilder.UpdateData(
                table: "GradeLevels",
                keyColumn: "Id",
                keyValue: 10,
                column: "Amount",
                value: 25000m);

            migrationBuilder.UpdateData(
                table: "GradeLevels",
                keyColumn: "Id",
                keyValue: 11,
                column: "Amount",
                value: 26500m);

            migrationBuilder.UpdateData(
                table: "GradeLevels",
                keyColumn: "Id",
                keyValue: 12,
                column: "Amount",
                value: 28000m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Amount",
                table: "GradeLevels");
        }
    }
}
