using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Security.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMultiTenancy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_users_normalized_email",
                schema: "security",
                table: "users");

            migrationBuilder.DropIndex(
                name: "ix_roles_normalized_name",
                schema: "security",
                table: "roles");

            migrationBuilder.DropIndex(
                name: "ix_refresh_sessions_user_id",
                schema: "security",
                table: "refresh_sessions");

            migrationBuilder.DropIndex(
                name: "ix_refresh_sessions_user_id_revoked",
                schema: "security",
                table: "refresh_sessions");

            migrationBuilder.DropIndex(
                name: "ix_password_reset_tokens_user_id",
                schema: "security",
                table: "password_reset_tokens");

            migrationBuilder.DropIndex(
                name: "ix_password_change_tokens_user_id",
                schema: "security",
                table: "password_change_tokens");

            migrationBuilder.DropIndex(
                name: "ix_email_verification_tokens_user_id",
                schema: "security",
                table: "email_verification_tokens");

            migrationBuilder.DropIndex(
                name: "ix_email_change_tokens_user_id",
                schema: "security",
                table: "email_change_tokens");

            migrationBuilder.DropIndex(
                name: "ix_audit_logs_created_at_utc",
                schema: "security",
                table: "audit_logs");

            migrationBuilder.DropIndex(
                name: "ix_audit_logs_user_id",
                schema: "security",
                table: "audit_logs");

            migrationBuilder.DropIndex(
                name: "ix_audit_logs_user_id_created_at_utc",
                schema: "security",
                table: "audit_logs");

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                schema: "security",
                table: "users",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000001"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                schema: "security",
                table: "user_roles",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000001"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                schema: "security",
                table: "roles",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000001"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                schema: "security",
                table: "role_permissions",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000001"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                schema: "security",
                table: "refresh_tokens",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000001"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                schema: "security",
                table: "refresh_sessions",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000001"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                schema: "security",
                table: "recovery_codes",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000001"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                schema: "security",
                table: "password_reset_tokens",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000001"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                schema: "security",
                table: "password_change_tokens",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000001"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                schema: "security",
                table: "mfa_methods",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000001"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                schema: "security",
                table: "email_verification_tokens",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000001"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                schema: "security",
                table: "email_change_tokens",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000001"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                schema: "security",
                table: "audit_logs",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000001"));

            migrationBuilder.CreateTable(
                name: "tenants",
                schema: "security",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Slug = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tenants", x => x.Id);
                });

            // Seed the well-known default tenant. Existing rows are backfilled to this tenant
            // (see the TenantId column defaults above) and single-tenant mode always uses it.
            migrationBuilder.InsertData(
                schema: "security",
                table: "tenants",
                columns: new[] { "Id", "Name", "Slug", "IsActive", "CreatedAtUtc" },
                values: new object[]
                {
                    new Guid("00000000-0000-0000-0000-000000000001"),
                    "Default",
                    "default",
                    true,
                    new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                });

            migrationBuilder.CreateIndex(
                name: "ix_users_tenant_id_normalized_email",
                schema: "security",
                table: "users",
                columns: new[] { "TenantId", "NormalizedEmail" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_roles_tenant_id_normalized_name",
                schema: "security",
                table: "roles",
                columns: new[] { "TenantId", "NormalizedName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_refresh_sessions_tenant_id_user_id",
                schema: "security",
                table: "refresh_sessions",
                columns: new[] { "TenantId", "UserId" });

            migrationBuilder.CreateIndex(
                name: "ix_refresh_sessions_tenant_id_user_id_revoked",
                schema: "security",
                table: "refresh_sessions",
                columns: new[] { "TenantId", "UserId", "Revoked" });

            migrationBuilder.CreateIndex(
                name: "ix_password_reset_tokens_tenant_id_user_id",
                schema: "security",
                table: "password_reset_tokens",
                columns: new[] { "TenantId", "UserId" });

            migrationBuilder.CreateIndex(
                name: "ix_password_change_tokens_tenant_id_user_id",
                schema: "security",
                table: "password_change_tokens",
                columns: new[] { "TenantId", "UserId" });

            migrationBuilder.CreateIndex(
                name: "ix_email_verification_tokens_tenant_id_user_id",
                schema: "security",
                table: "email_verification_tokens",
                columns: new[] { "TenantId", "UserId" });

            migrationBuilder.CreateIndex(
                name: "ix_email_change_tokens_tenant_id_user_id",
                schema: "security",
                table: "email_change_tokens",
                columns: new[] { "TenantId", "UserId" });

            migrationBuilder.CreateIndex(
                name: "ix_audit_logs_tenant_id_created_at_utc",
                schema: "security",
                table: "audit_logs",
                columns: new[] { "TenantId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "ix_audit_logs_tenant_id_user_id",
                schema: "security",
                table: "audit_logs",
                columns: new[] { "TenantId", "UserId" });

            migrationBuilder.CreateIndex(
                name: "ix_audit_logs_tenant_id_user_id_created_at_utc",
                schema: "security",
                table: "audit_logs",
                columns: new[] { "TenantId", "UserId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "ix_tenants_slug",
                schema: "security",
                table: "tenants",
                column: "Slug",
                unique: true);

            // The column defaults above existed only to backfill pre-existing rows to the default
            // tenant. New rows always receive their TenantId from the tenant save interceptor, so we
            // drop the residual database defaults to keep the schema aligned with the model snapshot.
            migrationBuilder.Sql(
                """
                ALTER TABLE security.users ALTER COLUMN "TenantId" DROP DEFAULT;
                ALTER TABLE security.user_roles ALTER COLUMN "TenantId" DROP DEFAULT;
                ALTER TABLE security.roles ALTER COLUMN "TenantId" DROP DEFAULT;
                ALTER TABLE security.role_permissions ALTER COLUMN "TenantId" DROP DEFAULT;
                ALTER TABLE security.refresh_tokens ALTER COLUMN "TenantId" DROP DEFAULT;
                ALTER TABLE security.refresh_sessions ALTER COLUMN "TenantId" DROP DEFAULT;
                ALTER TABLE security.recovery_codes ALTER COLUMN "TenantId" DROP DEFAULT;
                ALTER TABLE security.password_reset_tokens ALTER COLUMN "TenantId" DROP DEFAULT;
                ALTER TABLE security.password_change_tokens ALTER COLUMN "TenantId" DROP DEFAULT;
                ALTER TABLE security.mfa_methods ALTER COLUMN "TenantId" DROP DEFAULT;
                ALTER TABLE security.email_verification_tokens ALTER COLUMN "TenantId" DROP DEFAULT;
                ALTER TABLE security.email_change_tokens ALTER COLUMN "TenantId" DROP DEFAULT;
                ALTER TABLE security.audit_logs ALTER COLUMN "TenantId" DROP DEFAULT;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tenants",
                schema: "security");

            migrationBuilder.DropIndex(
                name: "ix_users_tenant_id_normalized_email",
                schema: "security",
                table: "users");

            migrationBuilder.DropIndex(
                name: "ix_roles_tenant_id_normalized_name",
                schema: "security",
                table: "roles");

            migrationBuilder.DropIndex(
                name: "ix_refresh_sessions_tenant_id_user_id",
                schema: "security",
                table: "refresh_sessions");

            migrationBuilder.DropIndex(
                name: "ix_refresh_sessions_tenant_id_user_id_revoked",
                schema: "security",
                table: "refresh_sessions");

            migrationBuilder.DropIndex(
                name: "ix_password_reset_tokens_tenant_id_user_id",
                schema: "security",
                table: "password_reset_tokens");

            migrationBuilder.DropIndex(
                name: "ix_password_change_tokens_tenant_id_user_id",
                schema: "security",
                table: "password_change_tokens");

            migrationBuilder.DropIndex(
                name: "ix_email_verification_tokens_tenant_id_user_id",
                schema: "security",
                table: "email_verification_tokens");

            migrationBuilder.DropIndex(
                name: "ix_email_change_tokens_tenant_id_user_id",
                schema: "security",
                table: "email_change_tokens");

            migrationBuilder.DropIndex(
                name: "ix_audit_logs_tenant_id_created_at_utc",
                schema: "security",
                table: "audit_logs");

            migrationBuilder.DropIndex(
                name: "ix_audit_logs_tenant_id_user_id",
                schema: "security",
                table: "audit_logs");

            migrationBuilder.DropIndex(
                name: "ix_audit_logs_tenant_id_user_id_created_at_utc",
                schema: "security",
                table: "audit_logs");

            migrationBuilder.DropColumn(
                name: "TenantId",
                schema: "security",
                table: "users");

            migrationBuilder.DropColumn(
                name: "TenantId",
                schema: "security",
                table: "user_roles");

            migrationBuilder.DropColumn(
                name: "TenantId",
                schema: "security",
                table: "roles");

            migrationBuilder.DropColumn(
                name: "TenantId",
                schema: "security",
                table: "role_permissions");

            migrationBuilder.DropColumn(
                name: "TenantId",
                schema: "security",
                table: "refresh_tokens");

            migrationBuilder.DropColumn(
                name: "TenantId",
                schema: "security",
                table: "refresh_sessions");

            migrationBuilder.DropColumn(
                name: "TenantId",
                schema: "security",
                table: "recovery_codes");

            migrationBuilder.DropColumn(
                name: "TenantId",
                schema: "security",
                table: "password_reset_tokens");

            migrationBuilder.DropColumn(
                name: "TenantId",
                schema: "security",
                table: "password_change_tokens");

            migrationBuilder.DropColumn(
                name: "TenantId",
                schema: "security",
                table: "mfa_methods");

            migrationBuilder.DropColumn(
                name: "TenantId",
                schema: "security",
                table: "email_verification_tokens");

            migrationBuilder.DropColumn(
                name: "TenantId",
                schema: "security",
                table: "email_change_tokens");

            migrationBuilder.DropColumn(
                name: "TenantId",
                schema: "security",
                table: "audit_logs");

            migrationBuilder.CreateIndex(
                name: "ix_users_normalized_email",
                schema: "security",
                table: "users",
                column: "NormalizedEmail",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_roles_normalized_name",
                schema: "security",
                table: "roles",
                column: "NormalizedName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_refresh_sessions_user_id",
                schema: "security",
                table: "refresh_sessions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "ix_refresh_sessions_user_id_revoked",
                schema: "security",
                table: "refresh_sessions",
                columns: new[] { "UserId", "Revoked" });

            migrationBuilder.CreateIndex(
                name: "ix_password_reset_tokens_user_id",
                schema: "security",
                table: "password_reset_tokens",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "ix_password_change_tokens_user_id",
                schema: "security",
                table: "password_change_tokens",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "ix_email_verification_tokens_user_id",
                schema: "security",
                table: "email_verification_tokens",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "ix_email_change_tokens_user_id",
                schema: "security",
                table: "email_change_tokens",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "ix_audit_logs_created_at_utc",
                schema: "security",
                table: "audit_logs",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "ix_audit_logs_user_id",
                schema: "security",
                table: "audit_logs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "ix_audit_logs_user_id_created_at_utc",
                schema: "security",
                table: "audit_logs",
                columns: new[] { "UserId", "CreatedAtUtc" });
        }
    }
}
