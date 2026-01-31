using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyFicDB.Core.Migrations
{
    /// <inheritdoc />
    public partial class RestrictTagSeriesActorWhenInUse : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_tblStoryActors_tblActors_ActorId",
                table: "tblStoryActors");

            migrationBuilder.DropForeignKey(
                name: "FK_tblStorySeries_tblSeries_SeriesId",
                table: "tblStorySeries");

            migrationBuilder.DropForeignKey(
                name: "FK_tblStoryTags_tblTags_TagId",
                table: "tblStoryTags");

            migrationBuilder.AddForeignKey(
                name: "FK_tblStoryActors_tblActors_ActorId",
                table: "tblStoryActors",
                column: "ActorId",
                principalTable: "tblActors",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_tblStorySeries_tblSeries_SeriesId",
                table: "tblStorySeries",
                column: "SeriesId",
                principalTable: "tblSeries",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_tblStoryTags_tblTags_TagId",
                table: "tblStoryTags",
                column: "TagId",
                principalTable: "tblTags",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_tblStoryActors_tblActors_ActorId",
                table: "tblStoryActors");

            migrationBuilder.DropForeignKey(
                name: "FK_tblStorySeries_tblSeries_SeriesId",
                table: "tblStorySeries");

            migrationBuilder.DropForeignKey(
                name: "FK_tblStoryTags_tblTags_TagId",
                table: "tblStoryTags");

            migrationBuilder.AddForeignKey(
                name: "FK_tblStoryActors_tblActors_ActorId",
                table: "tblStoryActors",
                column: "ActorId",
                principalTable: "tblActors",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_tblStorySeries_tblSeries_SeriesId",
                table: "tblStorySeries",
                column: "SeriesId",
                principalTable: "tblSeries",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_tblStoryTags_tblTags_TagId",
                table: "tblStoryTags",
                column: "TagId",
                principalTable: "tblTags",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
