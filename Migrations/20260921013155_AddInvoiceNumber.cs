using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EduCore.Migrations
{
    /// <inheritdoc />
    public partial class AddInvoiceNumber : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Number",
                table: "Invoices",
                type: "nvarchar(16)",
                maxLength: 16,
                nullable: true);

            // Backfill numbers for any pre-existing invoices so none display blank. Id is the
            // monotonic identity, so formatting it keeps the number sequence in insertion order.
            migrationBuilder.Sql("""
                UPDATE [Invoices]
                SET [Number] = CONCAT('INV-', FORMAT([Id], 'D4'))
                WHERE [Number] IS NULL
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Number",
                table: "Invoices");
        }
    }
}
