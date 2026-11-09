using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Walletly.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTemporaryTenantsAndTenantStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "tenants",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Active");

            migrationBuilder.CreateTable(
                name: "temporary_tenants",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    first_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    last_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    password_hash = table.Column<string>(type: "text", nullable: true),
                    source = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    otp_code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    otp_expires_at = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    otp_attempts_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    invitation_token = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    invitation_expires_at = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    admin_notes = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    approved_at = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_temporary_tenants", x => x.id);
                    table.ForeignKey(
                        name: "FK_temporary_tenants_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_temporary_tenants_email",
                table: "temporary_tenants",
                column: "email");

            migrationBuilder.CreateIndex(
                name: "IX_temporary_tenants_invitation_token",
                table: "temporary_tenants",
                column: "invitation_token");

            migrationBuilder.CreateIndex(
                name: "IX_temporary_tenants_tenant_id",
                table: "temporary_tenants",
                column: "tenant_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "temporary_tenants");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "tenants");
        }
    }
}
