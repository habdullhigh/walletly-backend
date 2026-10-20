using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Walletly.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class ReshapePlatformRbacModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_platform_user_roles_platform_users_PlatformUserId",
                table: "platform_user_roles");

            migrationBuilder.DropIndex(
                name: "IX_roles_Name",
                table: "roles");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "roles");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "roles");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "permissions");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "permissions");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                table: "platform_users",
                newName: "LastLoginAt");

            migrationBuilder.RenameColumn(
                name: "PlatformUserId",
                table: "platform_user_roles",
                newName: "UserId");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "permissions",
                newName: "Code");

            migrationBuilder.RenameIndex(
                name: "IX_permissions_Name",
                table: "permissions",
                newName: "IX_permissions_Code");

            migrationBuilder.AddColumn<string>(
                name: "Code",
                table: "roles",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "platform_users",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.UpdateData(
                table: "permissions",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                column: "Code",
                value: "tenants.read");

            migrationBuilder.UpdateData(
                table: "permissions",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                column: "Code",
                value: "tenants.write");

            migrationBuilder.UpdateData(
                table: "permissions",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"),
                column: "Code",
                value: "wallets.read");

            migrationBuilder.UpdateData(
                table: "permissions",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444444"),
                column: "Code",
                value: "wallets.write");

            migrationBuilder.UpdateData(
                table: "permissions",
                keyColumn: "Id",
                keyValue: new Guid("55555555-5555-5555-5555-555555555555"),
                column: "Code",
                value: "platform_users.manage");

            migrationBuilder.UpdateData(
                table: "permissions",
                keyColumn: "Id",
                keyValue: new Guid("66666666-6666-6666-6666-666666666666"),
                column: "Code",
                value: "roles.manage");

            migrationBuilder.UpdateData(
                table: "roles",
                keyColumn: "Id",
                keyValue: new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                column: "Code",
                value: "admin");

            migrationBuilder.UpdateData(
                table: "roles",
                keyColumn: "Id",
                keyValue: new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
                column: "Code",
                value: "developer");

            migrationBuilder.UpdateData(
                table: "roles",
                keyColumn: "Id",
                keyValue: new Guid("cccccccc-cccc-cccc-cccc-cccccccccccc"),
                column: "Code",
                value: "support");

            migrationBuilder.CreateIndex(
                name: "IX_roles_Code",
                table: "roles",
                column: "Code",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_platform_user_roles_platform_users_UserId",
                table: "platform_user_roles",
                column: "UserId",
                principalTable: "platform_users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_platform_user_roles_platform_users_UserId",
                table: "platform_user_roles");

            migrationBuilder.DropIndex(
                name: "IX_roles_Code",
                table: "roles");

            migrationBuilder.DropColumn(
                name: "Code",
                table: "roles");

            migrationBuilder.RenameColumn(
                name: "LastLoginAt",
                table: "platform_users",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "platform_user_roles",
                newName: "PlatformUserId");

            migrationBuilder.RenameColumn(
                name: "Code",
                table: "permissions",
                newName: "Name");

            migrationBuilder.RenameIndex(
                name: "IX_permissions_Code",
                table: "permissions",
                newName: "IX_permissions_Name");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "roles",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "roles",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "platform_users",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "permissions",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "permissions",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.UpdateData(
                table: "permissions",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                columns: new[] { "CreatedAt", "Description", "Name" },
                values: new object[] { new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "View tenants", "tenants:read" });

            migrationBuilder.UpdateData(
                table: "permissions",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                columns: new[] { "CreatedAt", "Description", "Name" },
                values: new object[] { new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Create/Edit tenants", "tenants:write" });

            migrationBuilder.UpdateData(
                table: "permissions",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"),
                columns: new[] { "CreatedAt", "Description", "Name" },
                values: new object[] { new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "View wallet states", "wallets:read" });

            migrationBuilder.UpdateData(
                table: "permissions",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444444"),
                columns: new[] { "CreatedAt", "Description", "Name" },
                values: new object[] { new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Manage wallets", "wallets:write" });

            migrationBuilder.UpdateData(
                table: "permissions",
                keyColumn: "Id",
                keyValue: new Guid("55555555-5555-5555-5555-555555555555"),
                columns: new[] { "CreatedAt", "Description", "Name" },
                values: new object[] { new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Manage platform users", "platform_users:manage" });

            migrationBuilder.UpdateData(
                table: "permissions",
                keyColumn: "Id",
                keyValue: new Guid("66666666-6666-6666-6666-666666666666"),
                columns: new[] { "CreatedAt", "Description", "Name" },
                values: new object[] { new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Manage roles & permissions", "roles:manage" });

            migrationBuilder.UpdateData(
                table: "roles",
                keyColumn: "Id",
                keyValue: new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                columns: new[] { "CreatedAt", "Description" },
                values: new object[] { new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Full platform administrator with all permissions" });

            migrationBuilder.UpdateData(
                table: "roles",
                keyColumn: "Id",
                keyValue: new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
                columns: new[] { "CreatedAt", "Description" },
                values: new object[] { new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Platform developer with tenant and system access" });

            migrationBuilder.UpdateData(
                table: "roles",
                keyColumn: "Id",
                keyValue: new Guid("cccccccc-cccc-cccc-cccc-cccccccccccc"),
                columns: new[] { "CreatedAt", "Description" },
                values: new object[] { new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Platform support agent" });

            migrationBuilder.CreateIndex(
                name: "IX_roles_Name",
                table: "roles",
                column: "Name",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_platform_user_roles_platform_users_PlatformUserId",
                table: "platform_user_roles",
                column: "PlatformUserId",
                principalTable: "platform_users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
