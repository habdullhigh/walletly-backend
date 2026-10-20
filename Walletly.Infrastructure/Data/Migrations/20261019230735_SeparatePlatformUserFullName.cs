using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Walletly.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class SeparatePlatformUserFullName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FirstName",
                table: "platform_users",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "LastName",
                table: "platform_users",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql(@"
                UPDATE platform_users
                SET ""FirstName"" = split_part(""FullName"", ' ', 1),
                    ""LastName"" = COALESCE(NULLIF(substr(""FullName"", length(split_part(""FullName"", ' ', 1)) + 2), ''), split_part(""FullName"", ' ', 1))
                WHERE ""FullName"" IS NOT NULL;
            ");

            migrationBuilder.DropColumn(
                name: "FullName",
                table: "platform_users");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FirstName",
                table: "platform_users");

            migrationBuilder.DropColumn(
                name: "LastName",
                table: "platform_users");

            migrationBuilder.AddColumn<string>(
                name: "FullName",
                table: "platform_users",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "");
        }
    }
}
