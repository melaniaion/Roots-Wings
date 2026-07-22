using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Roots_Wings.Migrations
{
    /// <inheritdoc />
    public partial class AddTherapyType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TherapyType",
                table: "TherapySessions",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TherapyType",
                table: "TherapySessions");
        }
    }
}
