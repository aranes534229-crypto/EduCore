using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EduCore.Migrations
{
    /// <inheritdoc />
    public partial class SimplifyInquiryStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Remap the old status ints onto the reduced enum { New=0, Approved=1, Rejected=2 }.
            // Unquoted identifiers keep this runnable on both the SQL Server localdb (dev) and SQLite (prod).
            migrationBuilder.Sql("UPDATE Inquiries SET Status = 1 WHERE Status = 4;");       // Converted  -> Approved
            migrationBuilder.Sql("UPDATE Inquiries SET Status = 2 WHERE Status = 3;");       // Rejected   -> Rejected(2)
            migrationBuilder.Sql("UPDATE Inquiries SET Status = 0 WHERE Status IN (1, 2);"); // InProgress/Closed -> New
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
