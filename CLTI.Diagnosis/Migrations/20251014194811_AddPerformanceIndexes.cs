using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CLTI.Diagnosis.Migrations
{
    /// <inheritdoc />
    public partial class AddPerformanceIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_sys_user_role_sys_user_SysUserId1",
                table: "sys_user_role");

            migrationBuilder.DropIndex(
                name: "IX_sys_user_role_SysUserId1",
                table: "sys_user_role");

            migrationBuilder.DropColumn(
                name: "SysUserId1",
                table: "sys_user_role");

            // Add performance indexes for login queries
            migrationBuilder.CreateIndex(
                name: "IX_sys_user_Email",
                table: "sys_user",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_sys_user_role_UserId_RoleId",
                table: "sys_user_role",
                columns: new[] { "SysUserId", "SysRoleId" });

            migrationBuilder.CreateIndex(
                name: "IX_sys_refresh_token_Token",
                table: "sys_refresh_token",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_sys_refresh_token_UserId_Active",
                table: "sys_refresh_token",
                columns: new[] { "UserId", "IsUsed", "IsRevoked" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop performance indexes
            migrationBuilder.DropIndex(
                name: "IX_sys_user_Email",
                table: "sys_user");

            migrationBuilder.DropIndex(
                name: "IX_sys_user_role_UserId_RoleId",
                table: "sys_user_role");

            migrationBuilder.DropIndex(
                name: "IX_sys_refresh_token_Token",
                table: "sys_refresh_token");

            migrationBuilder.DropIndex(
                name: "IX_sys_refresh_token_UserId_Active",
                table: "sys_refresh_token");

            migrationBuilder.AddColumn<int>(
                name: "SysUserId1",
                table: "sys_user_role",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_sys_user_role_SysUserId1",
                table: "sys_user_role",
                column: "SysUserId1");

            migrationBuilder.AddForeignKey(
                name: "FK_sys_user_role_sys_user_SysUserId1",
                table: "sys_user_role",
                column: "SysUserId1",
                principalTable: "sys_user",
                principalColumn: "Id");
        }
    }
}
