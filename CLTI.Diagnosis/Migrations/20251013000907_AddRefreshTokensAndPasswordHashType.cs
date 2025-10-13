using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CLTI.Diagnosis.Migrations
{
    /// <inheritdoc />
    public partial class AddRefreshTokensAndPasswordHashType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "sys_enum",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    OrderingType = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Guid = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    OrderingTypeEditor = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sys_enum", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "sys_log",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    Thread = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Level = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Logger = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Message = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Exception = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserId = table.Column<int>(type: "int", nullable: true),
                    ProcessId = table.Column<int>(type: "int", nullable: true),
                    Logger_namespace = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sys_log", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "sys_rights",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Guid = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sys_rights", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "sys_role",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Guid = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sys_role", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "sys_enum_item",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SysEnumId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Value = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Icon = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    OrderIndex = table.Column<int>(type: "int", nullable: false),
                    Guid = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    Color = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sys_enum_item", x => x.Id);
                    table.ForeignKey(
                        name: "FK_sys_enum_item_sys_enum_SysEnumId",
                        column: x => x.SysEnumId,
                        principalTable: "sys_enum",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "sys_role_rights",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SysRoleId = table.Column<int>(type: "int", nullable: false),
                    SysRightId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sys_role_rights", x => x.Id);
                    table.ForeignKey(
                        name: "FK_sys_role_rights_sys_rights_SysRightId",
                        column: x => x.SysRightId,
                        principalTable: "sys_rights",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_sys_role_rights_sys_role_SysRoleId",
                        column: x => x.SysRoleId,
                        principalTable: "sys_role",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "sys_api_key",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ApiKey = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Guid = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    StatusEnumItemId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sys_api_key", x => x.Id);
                    table.ForeignKey(
                        name: "FK_sys_api_key_sys_enum_item_StatusEnumItemId",
                        column: x => x.StatusEnumItemId,
                        principalTable: "sys_enum_item",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "sys_licence",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Guid = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    LicenceKey = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    LicenceTypeEnumItemId = table.Column<int>(type: "int", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    StatusEnumItemId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sys_licence", x => x.Id);
                    table.ForeignKey(
                        name: "FK_sys_licence_sys_enum_item_LicenceTypeEnumItemId",
                        column: x => x.LicenceTypeEnumItemId,
                        principalTable: "sys_enum_item",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_sys_licence_sys_enum_item_StatusEnumItemId",
                        column: x => x.StatusEnumItemId,
                        principalTable: "sys_enum_item",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "sys_user",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TitleBeforeName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    FirstName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    LastName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TitleAfterName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Password = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    StatusEnumItemId = table.Column<int>(type: "int", nullable: false),
                    PasswordHashType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Guid = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sys_user", x => x.Id);
                    table.ForeignKey(
                        name: "FK_sys_user_sys_enum_item_StatusEnumItemId",
                        column: x => x.StatusEnumItemId,
                        principalTable: "sys_enum_item",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "u_clti",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AbiKpi = table.Column<double>(type: "float", nullable: false),
                    FbiPpi = table.Column<double>(type: "float", nullable: true),
                    WifiCriteria_W1 = table.Column<bool>(type: "bit", nullable: false),
                    WifiCriteria_W2 = table.Column<bool>(type: "bit", nullable: false),
                    WifiCriteria_W3 = table.Column<bool>(type: "bit", nullable: false),
                    WifiCriteria_I0 = table.Column<bool>(type: "bit", nullable: false),
                    WifiCriteria_I1 = table.Column<bool>(type: "bit", nullable: false),
                    WifiCriteria_I2 = table.Column<bool>(type: "bit", nullable: false),
                    WifiCriteria_I3 = table.Column<bool>(type: "bit", nullable: false),
                    WifiCriteria_FI0 = table.Column<bool>(type: "bit", nullable: false),
                    WifiCriteria_FI1 = table.Column<bool>(type: "bit", nullable: false),
                    WifiCriteria_FI2 = table.Column<bool>(type: "bit", nullable: false),
                    WifiCriteria_FI3 = table.Column<bool>(type: "bit", nullable: false),
                    ClinicalStageWIfIEnumItemId = table.Column<int>(type: "int", nullable: false),
                    CrabPoints = table.Column<int>(type: "int", nullable: false),
                    TwoYLE = table.Column<double>(type: "float", nullable: false),
                    GlassCriteria_AidI = table.Column<bool>(type: "bit", nullable: false),
                    GlassCriteria_AidII = table.Column<bool>(type: "bit", nullable: false),
                    GlassCriteria_AidA = table.Column<bool>(type: "bit", nullable: false),
                    GlassCriteria_AidB = table.Column<bool>(type: "bit", nullable: false),
                    GlassCriteria_Fps = table.Column<int>(type: "int", nullable: false),
                    GlassCriteria_Ips = table.Column<int>(type: "int", nullable: false),
                    GlassCriteria_Iid = table.Column<bool>(type: "bit", nullable: false),
                    GlassCriteria_IidI = table.Column<bool>(type: "bit", nullable: false),
                    GlassCriteria_IidII = table.Column<bool>(type: "bit", nullable: false),
                    GlassCriteria_IidIII = table.Column<bool>(type: "bit", nullable: false),
                    GlassCriteria_ImdP0 = table.Column<bool>(type: "bit", nullable: false),
                    GlassCriteria_ImdP1 = table.Column<bool>(type: "bit", nullable: false),
                    GlassCriteria_ImdP2 = table.Column<bool>(type: "bit", nullable: false),
                    Guid = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_u_clti", x => x.Id);
                    table.ForeignKey(
                        name: "FK_u_clti_sys_enum_item_ClinicalStageWIfIEnumItemId",
                        column: x => x.ClinicalStageWIfIEnumItemId,
                        principalTable: "sys_enum_item",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "sys_refresh_token",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Token = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsRevoked = table.Column<bool>(type: "bit", nullable: false),
                    IsUsed = table.Column<bool>(type: "bit", nullable: false),
                    ReplacedByToken = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Guid = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sys_refresh_token", x => x.Id);
                    table.ForeignKey(
                        name: "FK_sys_refresh_token_sys_user_UserId",
                        column: x => x.UserId,
                        principalTable: "sys_user",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "sys_user_licence",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Guid = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    LicenceId = table.Column<int>(type: "int", nullable: false),
                    AssignedDate = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    ExpiryDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    StatusEnumItemId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sys_user_licence", x => x.Id);
                    table.ForeignKey(
                        name: "FK_sys_user_licence_sys_enum_item_StatusEnumItemId",
                        column: x => x.StatusEnumItemId,
                        principalTable: "sys_enum_item",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_sys_user_licence_sys_licence_LicenceId",
                        column: x => x.LicenceId,
                        principalTable: "sys_licence",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_sys_user_licence_sys_user_UserId",
                        column: x => x.UserId,
                        principalTable: "sys_user",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "sys_user_role",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SysUserId = table.Column<int>(type: "int", nullable: false),
                    SysRoleId = table.Column<int>(type: "int", nullable: false),
                    SysUserId1 = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sys_user_role", x => x.Id);
                    table.ForeignKey(
                        name: "FK_sys_user_role_sys_role_SysRoleId",
                        column: x => x.SysRoleId,
                        principalTable: "sys_role",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_sys_user_role_sys_user_SysUserId",
                        column: x => x.SysUserId,
                        principalTable: "sys_user",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_sys_user_role_sys_user_SysUserId1",
                        column: x => x.SysUserId1,
                        principalTable: "sys_user",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "u_clti_photos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CltiCaseId = table.Column<int>(type: "int", nullable: false),
                    CltiCaseGuid = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Guid = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    CTA = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    DSA = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    MRA = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    US_of_lower_extremity_arteries = table.Column<byte[]>(type: "varbinary(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_u_clti_photos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_u_clti_photos_u_clti_CltiCaseId",
                        column: x => x.CltiCaseId,
                        principalTable: "u_clti",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_sys_api_key_StatusEnumItemId",
                table: "sys_api_key",
                column: "StatusEnumItemId");

            migrationBuilder.CreateIndex(
                name: "IX_sys_enum_item_SysEnumId",
                table: "sys_enum_item",
                column: "SysEnumId");

            migrationBuilder.CreateIndex(
                name: "IX_sys_licence_LicenceTypeEnumItemId",
                table: "sys_licence",
                column: "LicenceTypeEnumItemId");

            migrationBuilder.CreateIndex(
                name: "IX_sys_licence_StatusEnumItemId",
                table: "sys_licence",
                column: "StatusEnumItemId");

            migrationBuilder.CreateIndex(
                name: "IX_sys_refresh_token_UserId",
                table: "sys_refresh_token",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_sys_role_rights_SysRightId",
                table: "sys_role_rights",
                column: "SysRightId");

            migrationBuilder.CreateIndex(
                name: "IX_sys_role_rights_SysRoleId",
                table: "sys_role_rights",
                column: "SysRoleId");

            migrationBuilder.CreateIndex(
                name: "IX_sys_user_StatusEnumItemId",
                table: "sys_user",
                column: "StatusEnumItemId");

            migrationBuilder.CreateIndex(
                name: "IX_sys_user_licence_LicenceId",
                table: "sys_user_licence",
                column: "LicenceId");

            migrationBuilder.CreateIndex(
                name: "IX_sys_user_licence_StatusEnumItemId",
                table: "sys_user_licence",
                column: "StatusEnumItemId");

            migrationBuilder.CreateIndex(
                name: "IX_sys_user_licence_UserId",
                table: "sys_user_licence",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_sys_user_role_SysRoleId",
                table: "sys_user_role",
                column: "SysRoleId");

            migrationBuilder.CreateIndex(
                name: "IX_sys_user_role_SysUserId",
                table: "sys_user_role",
                column: "SysUserId");

            migrationBuilder.CreateIndex(
                name: "IX_sys_user_role_SysUserId1",
                table: "sys_user_role",
                column: "SysUserId1");

            migrationBuilder.CreateIndex(
                name: "IX_u_clti_ClinicalStageWIfIEnumItemId",
                table: "u_clti",
                column: "ClinicalStageWIfIEnumItemId");

            migrationBuilder.CreateIndex(
                name: "IX_u_clti_photos_CltiCaseId",
                table: "u_clti_photos",
                column: "CltiCaseId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "sys_api_key");

            migrationBuilder.DropTable(
                name: "sys_log");

            migrationBuilder.DropTable(
                name: "sys_refresh_token");

            migrationBuilder.DropTable(
                name: "sys_role_rights");

            migrationBuilder.DropTable(
                name: "sys_user_licence");

            migrationBuilder.DropTable(
                name: "sys_user_role");

            migrationBuilder.DropTable(
                name: "u_clti_photos");

            migrationBuilder.DropTable(
                name: "sys_rights");

            migrationBuilder.DropTable(
                name: "sys_licence");

            migrationBuilder.DropTable(
                name: "sys_role");

            migrationBuilder.DropTable(
                name: "sys_user");

            migrationBuilder.DropTable(
                name: "u_clti");

            migrationBuilder.DropTable(
                name: "sys_enum_item");

            migrationBuilder.DropTable(
                name: "sys_enum");
        }
    }
}
