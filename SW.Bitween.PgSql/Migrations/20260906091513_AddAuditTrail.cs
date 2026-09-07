using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SW.Bitween.PgSql.Migrations
{
    /// <inheritdoc />
    public partial class AddAuditTrail : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AuditEntries",
                schema: "infolink",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(32)", unicode: false, maxLength: 32, nullable: false),
                    correlation_id = table.Column<string>(type: "character varying(36)", unicode: false, maxLength: 36, nullable: false),
                    sequence = table.Column<int>(type: "integer", nullable: false),
                    occurred_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    user_id = table.Column<string>(type: "character varying(50)", unicode: false, maxLength: 50, nullable: true),
                    entity_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    entity_key = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    state = table.Column<string>(type: "character varying(10)", unicode: false, maxLength: 10, nullable: false),
                    changes = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_audit_entries", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_audit_entries_correlation_id",
                schema: "infolink",
                table: "AuditEntries",
                column: "correlation_id");

            migrationBuilder.CreateIndex(
                name: "ix_audit_entries_entity_name_entity_key_occurred_on",
                schema: "infolink",
                table: "AuditEntries",
                columns: new[] { "entity_name", "entity_key", "occurred_on" });

            migrationBuilder.CreateIndex(
                name: "ix_audit_entries_occurred_on",
                schema: "infolink",
                table: "AuditEntries",
                column: "occurred_on");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditEntries",
                schema: "infolink");
        }
    }
}
