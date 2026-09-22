using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EduCore.Migrations
{
    /// <inheritdoc />
    public partial class ConvertStudentNumberToSequential : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Convert existing student numbers from year-prefixed (2026-0001) to sequential (1001).
            // The sequence number is the right-half of the old format, shifted by 1000.
            migrationBuilder.Sql("""
                UPDATE [Students]
                SET [StudentNumber] = (1000 + CAST(RIGHT([StudentNumber], 4) AS INT))
                WHERE [StudentNumber] LIKE '____-____'
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
