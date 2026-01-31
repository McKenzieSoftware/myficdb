using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyFicDB.Core.Migrations
{
    /// <inheritdoc />
    public partial class RenameDefaultDbTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                table: "AspNetRoleClaims");

            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                table: "AspNetUserClaims");

            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                table: "AspNetUserLogins");

            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                table: "AspNetUserRoles");

            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                table: "AspNetUserRoles");

            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                table: "AspNetUserTokens");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AspNetUserTokens",
                table: "AspNetUserTokens");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AspNetUsers",
                table: "AspNetUsers");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AspNetUserRoles",
                table: "AspNetUserRoles");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AspNetUserLogins",
                table: "AspNetUserLogins");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AspNetUserClaims",
                table: "AspNetUserClaims");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AspNetRoles",
                table: "AspNetRoles");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AspNetRoleClaims",
                table: "AspNetRoleClaims");

            migrationBuilder.RenameTable(
                name: "AspNetUserTokens",
                newName: "tblSystemUserTokens");

            migrationBuilder.RenameTable(
                name: "AspNetUsers",
                newName: "tblSystemUser");

            migrationBuilder.RenameTable(
                name: "AspNetUserRoles",
                newName: "tblSystemUserRoles");

            migrationBuilder.RenameTable(
                name: "AspNetUserLogins",
                newName: "tblSystemUserLogins");

            migrationBuilder.RenameTable(
                name: "AspNetUserClaims",
                newName: "tblSystemUserClaims");

            migrationBuilder.RenameTable(
                name: "AspNetRoles",
                newName: "tblSystemRoles");

            migrationBuilder.RenameTable(
                name: "AspNetRoleClaims",
                newName: "tblSystemRoleClaims");

            migrationBuilder.RenameIndex(
                name: "IX_AspNetUserRoles_RoleId",
                table: "tblSystemUserRoles",
                newName: "IX_tblSystemUserRoles_RoleId");

            migrationBuilder.RenameIndex(
                name: "IX_AspNetUserLogins_UserId",
                table: "tblSystemUserLogins",
                newName: "IX_tblSystemUserLogins_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_AspNetUserClaims_UserId",
                table: "tblSystemUserClaims",
                newName: "IX_tblSystemUserClaims_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_AspNetRoleClaims_RoleId",
                table: "tblSystemRoleClaims",
                newName: "IX_tblSystemRoleClaims_RoleId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_tblSystemUserTokens",
                table: "tblSystemUserTokens",
                columns: new[] { "UserId", "LoginProvider", "Name" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_tblSystemUser",
                table: "tblSystemUser",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_tblSystemUserRoles",
                table: "tblSystemUserRoles",
                columns: new[] { "UserId", "RoleId" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_tblSystemUserLogins",
                table: "tblSystemUserLogins",
                columns: new[] { "LoginProvider", "ProviderKey" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_tblSystemUserClaims",
                table: "tblSystemUserClaims",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_tblSystemRoles",
                table: "tblSystemRoles",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_tblSystemRoleClaims",
                table: "tblSystemRoleClaims",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_tblSystemRoleClaims_tblSystemRoles_RoleId",
                table: "tblSystemRoleClaims",
                column: "RoleId",
                principalTable: "tblSystemRoles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_tblSystemUserClaims_tblSystemUser_UserId",
                table: "tblSystemUserClaims",
                column: "UserId",
                principalTable: "tblSystemUser",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_tblSystemUserLogins_tblSystemUser_UserId",
                table: "tblSystemUserLogins",
                column: "UserId",
                principalTable: "tblSystemUser",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_tblSystemUserRoles_tblSystemRoles_RoleId",
                table: "tblSystemUserRoles",
                column: "RoleId",
                principalTable: "tblSystemRoles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_tblSystemUserRoles_tblSystemUser_UserId",
                table: "tblSystemUserRoles",
                column: "UserId",
                principalTable: "tblSystemUser",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_tblSystemUserTokens_tblSystemUser_UserId",
                table: "tblSystemUserTokens",
                column: "UserId",
                principalTable: "tblSystemUser",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_tblSystemRoleClaims_tblSystemRoles_RoleId",
                table: "tblSystemRoleClaims");

            migrationBuilder.DropForeignKey(
                name: "FK_tblSystemUserClaims_tblSystemUser_UserId",
                table: "tblSystemUserClaims");

            migrationBuilder.DropForeignKey(
                name: "FK_tblSystemUserLogins_tblSystemUser_UserId",
                table: "tblSystemUserLogins");

            migrationBuilder.DropForeignKey(
                name: "FK_tblSystemUserRoles_tblSystemRoles_RoleId",
                table: "tblSystemUserRoles");

            migrationBuilder.DropForeignKey(
                name: "FK_tblSystemUserRoles_tblSystemUser_UserId",
                table: "tblSystemUserRoles");

            migrationBuilder.DropForeignKey(
                name: "FK_tblSystemUserTokens_tblSystemUser_UserId",
                table: "tblSystemUserTokens");

            migrationBuilder.DropPrimaryKey(
                name: "PK_tblSystemUserTokens",
                table: "tblSystemUserTokens");

            migrationBuilder.DropPrimaryKey(
                name: "PK_tblSystemUserRoles",
                table: "tblSystemUserRoles");

            migrationBuilder.DropPrimaryKey(
                name: "PK_tblSystemUserLogins",
                table: "tblSystemUserLogins");

            migrationBuilder.DropPrimaryKey(
                name: "PK_tblSystemUserClaims",
                table: "tblSystemUserClaims");

            migrationBuilder.DropPrimaryKey(
                name: "PK_tblSystemUser",
                table: "tblSystemUser");

            migrationBuilder.DropPrimaryKey(
                name: "PK_tblSystemRoles",
                table: "tblSystemRoles");

            migrationBuilder.DropPrimaryKey(
                name: "PK_tblSystemRoleClaims",
                table: "tblSystemRoleClaims");

            migrationBuilder.RenameTable(
                name: "tblSystemUserTokens",
                newName: "AspNetUserTokens");

            migrationBuilder.RenameTable(
                name: "tblSystemUserRoles",
                newName: "AspNetUserRoles");

            migrationBuilder.RenameTable(
                name: "tblSystemUserLogins",
                newName: "AspNetUserLogins");

            migrationBuilder.RenameTable(
                name: "tblSystemUserClaims",
                newName: "AspNetUserClaims");

            migrationBuilder.RenameTable(
                name: "tblSystemUser",
                newName: "AspNetUsers");

            migrationBuilder.RenameTable(
                name: "tblSystemRoles",
                newName: "AspNetRoles");

            migrationBuilder.RenameTable(
                name: "tblSystemRoleClaims",
                newName: "AspNetRoleClaims");

            migrationBuilder.RenameIndex(
                name: "IX_tblSystemUserRoles_RoleId",
                table: "AspNetUserRoles",
                newName: "IX_AspNetUserRoles_RoleId");

            migrationBuilder.RenameIndex(
                name: "IX_tblSystemUserLogins_UserId",
                table: "AspNetUserLogins",
                newName: "IX_AspNetUserLogins_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_tblSystemUserClaims_UserId",
                table: "AspNetUserClaims",
                newName: "IX_AspNetUserClaims_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_tblSystemRoleClaims_RoleId",
                table: "AspNetRoleClaims",
                newName: "IX_AspNetRoleClaims_RoleId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AspNetUserTokens",
                table: "AspNetUserTokens",
                columns: new[] { "UserId", "LoginProvider", "Name" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_AspNetUserRoles",
                table: "AspNetUserRoles",
                columns: new[] { "UserId", "RoleId" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_AspNetUserLogins",
                table: "AspNetUserLogins",
                columns: new[] { "LoginProvider", "ProviderKey" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_AspNetUserClaims",
                table: "AspNetUserClaims",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AspNetUsers",
                table: "AspNetUsers",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AspNetRoles",
                table: "AspNetRoles",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AspNetRoleClaims",
                table: "AspNetRoleClaims",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                table: "AspNetRoleClaims",
                column: "RoleId",
                principalTable: "AspNetRoles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                table: "AspNetUserClaims",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                table: "AspNetUserLogins",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                table: "AspNetUserRoles",
                column: "RoleId",
                principalTable: "AspNetRoles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                table: "AspNetUserRoles",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                table: "AspNetUserTokens",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
