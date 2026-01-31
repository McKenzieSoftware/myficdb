using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyFicDB.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddReadCounterToStory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ReadCounter",
                table: "tblStories",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReadCounter",
                table: "tblStories");
        }
    }
}
