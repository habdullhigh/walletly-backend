using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Walletly.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddWaitlistQualifiers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "expected_monthly_volume",
                table: "temporary_tenants",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string[]>(
                name: "use_cases",
                table: "temporary_tenants",
                type: "text[]",
                nullable: false,
                defaultValueSql: "'{}'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "expected_monthly_volume",
                table: "temporary_tenants");

            migrationBuilder.DropColumn(
                name: "use_cases",
                table: "temporary_tenants");
        }
    }
}
