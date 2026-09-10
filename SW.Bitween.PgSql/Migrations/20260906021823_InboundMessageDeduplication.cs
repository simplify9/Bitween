using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SW.Bitween.PgSql.Migrations
{
    /// <inheritdoc />
    public partial class InboundMessageDeduplication : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "deduplication_window_days",
                schema: "infolink",
                table: "data_source",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "inbound_message",
                schema: "infolink",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: false),
                    data_source_id = table.Column<int>(type: "integer", nullable: false),
                    xchange_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    seen_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_inbound_message", x => x.id);
                    table.ForeignKey(
                        name: "fk_inbound_message_data_source_data_source_id",
                        column: x => x.data_source_id,
                        principalSchema: "infolink",
                        principalTable: "data_source",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_inbound_message_data_source_id",
                schema: "infolink",
                table: "inbound_message",
                column: "data_source_id");

            migrationBuilder.CreateIndex(
                name: "ix_inbound_message_seen_on",
                schema: "infolink",
                table: "inbound_message",
                column: "seen_on");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "inbound_message",
                schema: "infolink");

            migrationBuilder.DropColumn(
                name: "deduplication_window_days",
                schema: "infolink",
                table: "data_source");
        }
    }
}
