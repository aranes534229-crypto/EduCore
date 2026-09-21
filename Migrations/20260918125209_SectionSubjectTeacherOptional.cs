using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EduCore.Migrations
{
    /// <inheritdoc />
    public partial class SectionSubjectTeacherOptional : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SectionSubjects_Faculty_FacultyId",
                table: "SectionSubjects");

            migrationBuilder.AlterColumn<int>(
                name: "FacultyId",
                table: "SectionSubjects",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddForeignKey(
                name: "FK_SectionSubjects_Faculty_FacultyId",
                table: "SectionSubjects",
                column: "FacultyId",
                principalTable: "Faculty",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SectionSubjects_Faculty_FacultyId",
                table: "SectionSubjects");

            migrationBuilder.AlterColumn<int>(
                name: "FacultyId",
                table: "SectionSubjects",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_SectionSubjects_Faculty_FacultyId",
                table: "SectionSubjects",
                column: "FacultyId",
                principalTable: "Faculty",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
