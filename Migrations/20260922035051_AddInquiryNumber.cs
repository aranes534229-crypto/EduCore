using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EduCore.Migrations
{
    /// <inheritdoc />
    public partial class AddInquiryNumber : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "InquiryNumber",
                table: "Inquiries",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "");

            // Backfill existing inquiries in insertion order so Karla Reyes gets INQ-1001,
            // Sofia Dela Cruz INQ-1002, Matthew Loe INQ-1003, and so on.
            migrationBuilder.Sql("""
                WITH Ordered AS (
                    SELECT [Id], ROW_NUMBER() OVER (ORDER BY [Id]) AS RowNum
                    FROM [Inquiries]
                )
                UPDATE i
                SET [InquiryNumber] = CONCAT('INQ-', 1000 + RowNum)
                FROM [Inquiries] i
                INNER JOIN Ordered o ON o.[Id] = i.[Id]
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "InquiryNumber",
                table: "Inquiries");
        }
    }
}
