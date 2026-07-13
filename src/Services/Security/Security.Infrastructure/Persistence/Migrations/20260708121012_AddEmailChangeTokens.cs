using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Security.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEmailChangeTokens : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "email_change_tokens",
                schema: "security",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    TokenHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Used = table.Column<bool>(type: "boolean", nullable: false),
                    UsedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_email_change_tokens", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_email_change_tokens_expires_at_utc",
                schema: "security",
                table: "email_change_tokens",
                column: "ExpiresAtUtc");

            migrationBuilder.CreateIndex(
                name: "ix_email_change_tokens_token_hash",
                schema: "security",
                table: "email_change_tokens",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_email_change_tokens_user_id",
                schema: "security",
                table: "email_change_tokens",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "email_change_tokens",
                schema: "security");
        }
    }
}
