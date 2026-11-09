using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Walletly.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTenantEnvironmentScopedUsersAndRoles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_tenant_users_tenants_TenantId",
                table: "tenant_users");

            migrationBuilder.DropIndex(
                name: "IX_tenant_users_Email",
                table: "tenant_users");

            migrationBuilder.DropIndex(
                name: "IX_tenant_users_TenantId",
                table: "tenant_users");

            migrationBuilder.DropColumn(
                name: "LastLoginAt",
                table: "tenant_users");

            migrationBuilder.DropColumn(
                name: "PasswordHash",
                table: "tenant_users");

            migrationBuilder.RenameColumn(
                name: "TenantId",
                table: "tenant_users",
                newName: "TenantEnvironmentId");

            migrationBuilder.AddColumn<string>(
                name: "ExternalReference",
                table: "tenant_users",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TenantRoleId",
                table: "tenant_users",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "tenant_account_owners",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: false),
                    FirstName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Role = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastLoginAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tenant_account_owners", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tenant_account_owners_tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tenant_roles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantEnvironmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tenant_roles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tenant_roles_tenant_environments_TenantEnvironmentId",
                        column: x => x.TenantEnvironmentId,
                        principalTable: "tenant_environments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tenant_role_permissions",
                columns: table => new
                {
                    RoleId = table.Column<Guid>(type: "uuid", nullable: false),
                    Permission = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tenant_role_permissions", x => new { x.RoleId, x.Permission });
                    table.ForeignKey(
                        name: "FK_tenant_role_permissions_tenant_roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "tenant_roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_tenant_users_TenantEnvironmentId_Email",
                table: "tenant_users",
                columns: new[] { "TenantEnvironmentId", "Email" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tenant_users_TenantEnvironmentId_ExternalReference",
                table: "tenant_users",
                columns: new[] { "TenantEnvironmentId", "ExternalReference" });

            migrationBuilder.CreateIndex(
                name: "IX_tenant_users_TenantRoleId",
                table: "tenant_users",
                column: "TenantRoleId");

            migrationBuilder.CreateIndex(
                name: "IX_tenant_account_owners_Email",
                table: "tenant_account_owners",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tenant_account_owners_TenantId",
                table: "tenant_account_owners",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_tenant_roles_TenantEnvironmentId_Code",
                table: "tenant_roles",
                columns: new[] { "TenantEnvironmentId", "Code" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_tenant_users_tenant_environments_TenantEnvironmentId",
                table: "tenant_users",
                column: "TenantEnvironmentId",
                principalTable: "tenant_environments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_tenant_users_tenant_roles_TenantRoleId",
                table: "tenant_users",
                column: "TenantRoleId",
                principalTable: "tenant_roles",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_tenant_users_tenant_environments_TenantEnvironmentId",
                table: "tenant_users");

            migrationBuilder.DropForeignKey(
                name: "FK_tenant_users_tenant_roles_TenantRoleId",
                table: "tenant_users");

            migrationBuilder.DropTable(
                name: "tenant_account_owners");

            migrationBuilder.DropTable(
                name: "tenant_role_permissions");

            migrationBuilder.DropTable(
                name: "tenant_roles");

            migrationBuilder.DropIndex(
                name: "IX_tenant_users_TenantEnvironmentId_Email",
                table: "tenant_users");

            migrationBuilder.DropIndex(
                name: "IX_tenant_users_TenantEnvironmentId_ExternalReference",
                table: "tenant_users");

            migrationBuilder.DropIndex(
                name: "IX_tenant_users_TenantRoleId",
                table: "tenant_users");

            migrationBuilder.DropColumn(
                name: "ExternalReference",
                table: "tenant_users");

            migrationBuilder.DropColumn(
                name: "TenantRoleId",
                table: "tenant_users");

            migrationBuilder.RenameColumn(
                name: "TenantEnvironmentId",
                table: "tenant_users",
                newName: "TenantId");

            migrationBuilder.AddColumn<DateTime>(
                name: "LastLoginAt",
                table: "tenant_users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PasswordHash",
                table: "tenant_users",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_tenant_users_Email",
                table: "tenant_users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tenant_users_TenantId",
                table: "tenant_users",
                column: "TenantId");

            migrationBuilder.AddForeignKey(
                name: "FK_tenant_users_tenants_TenantId",
                table: "tenant_users",
                column: "TenantId",
                principalTable: "tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
