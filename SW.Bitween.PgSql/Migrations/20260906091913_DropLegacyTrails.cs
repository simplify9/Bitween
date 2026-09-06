using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SW.Bitween.PgSql.Migrations
{
    /// <inheritdoc />
    public partial class DropLegacyTrails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "document_trail",
                schema: "infolink");

            migrationBuilder.DropTable(
                name: "subscription_trail",
                schema: "infolink");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "document_trail",
                schema: "infolink",
                columns: table => new
                {
                    id = table.Column<string>(type: "text", nullable: false),
                    document_id = table.Column<int>(type: "integer", nullable: false),
                    code = table.Column<int>(type: "integer", nullable: false),
                    created_by = table.Column<string>(type: "text", nullable: true),
                    created_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    state_after = table.Column<string>(type: "text", nullable: true),
                    state_before = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_document_trail", x => x.id);
                    table.ForeignKey(
                        name: "fk_document_trail_document_document_id",
                        column: x => x.document_id,
                        principalSchema: "infolink",
                        principalTable: "document",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "subscription_trail",
                schema: "infolink",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    subscription_id = table.Column<int>(type: "integer", nullable: false),
                    code = table.Column<int>(type: "integer", nullable: false),
                    created_by = table.Column<string>(type: "text", nullable: true),
                    created_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    state_after = table.Column<string>(type: "text", nullable: true),
                    state_before = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_subscription_trail", x => x.id);
                    table.ForeignKey(
                        name: "fk_subscription_trail_subscription_subscription_id",
                        column: x => x.subscription_id,
                        principalSchema: "infolink",
                        principalTable: "subscription",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_document_trail_created_on",
                schema: "infolink",
                table: "document_trail",
                column: "created_on");

            migrationBuilder.CreateIndex(
                name: "ix_document_trail_document_id",
                schema: "infolink",
                table: "document_trail",
                column: "document_id");

            migrationBuilder.CreateIndex(
                name: "ix_subscription_trail_created_on",
                schema: "infolink",
                table: "subscription_trail",
                column: "created_on");

            migrationBuilder.CreateIndex(
                name: "ix_subscription_trail_subscription_id",
                schema: "infolink",
                table: "subscription_trail",
                column: "subscription_id");
        }
    }
}
