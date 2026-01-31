using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyFicDB.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddNutCounterToStory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "NutCounter",
                table: "tblStories",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NutCounter",
                table: "tblStories");
        }
    }
}
