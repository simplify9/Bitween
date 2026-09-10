using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SW.Bitween.PgSql.Migrations
{
    /// <inheritdoc />
    public partial class ExternalBusDataSources : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:hstore", ",,");

            migrationBuilder.AddColumn<int>(
                name: "data_source_id",
                schema: "infolink",
                table: "bus_gateway",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "endpoint",
                schema: "infolink",
                table: "bus_gateway",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Dictionary<string, string>>(
                name: "endpoint_properties",
                schema: "infolink",
                table: "bus_gateway",
                type: "hstore",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "data_source",
                schema: "infolink",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "text", nullable: true),
                    adapter_id = table.Column<string>(type: "text", nullable: true),
                    kind = table.Column<int>(type: "integer", nullable: false),
                    properties = table.Column<Dictionary<string, string>>(type: "hstore", nullable: true),
                    secret_properties = table.Column<List<string>>(type: "text[]", nullable: true),
                    inactive = table.Column<bool>(type: "boolean", nullable: false),
                    last_known_state = table.Column<string>(type: "text", nullable: true),
                    last_heartbeat_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_exception = table.Column<string>(type: "text", nullable: true),
                    consecutive_failures = table.Column<int>(type: "integer", nullable: false),
                    owned_by_node = table.Column<string>(type: "text", nullable: true),
                    created_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "text", nullable: true),
                    modified_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    modified_by = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_data_source", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_bus_gateway_data_source_id",
                schema: "infolink",
                table: "bus_gateway",
                column: "data_source_id");

            migrationBuilder.AddForeignKey(
                name: "fk_bus_gateway_data_source_data_source_id",
                schema: "infolink",
                table: "bus_gateway",
                column: "data_source_id",
                principalSchema: "infolink",
                principalTable: "data_source",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_bus_gateway_data_source_data_source_id",
                schema: "infolink",
                table: "bus_gateway");

            migrationBuilder.DropTable(
                name: "data_source",
                schema: "infolink");

            migrationBuilder.DropIndex(
                name: "ix_bus_gateway_data_source_id",
                schema: "infolink",
                table: "bus_gateway");

            migrationBuilder.DropColumn(
                name: "data_source_id",
                schema: "infolink",
                table: "bus_gateway");

            migrationBuilder.DropColumn(
                name: "endpoint",
                schema: "infolink",
                table: "bus_gateway");

            migrationBuilder.DropColumn(
                name: "endpoint_properties",
                schema: "infolink",
                table: "bus_gateway");

            migrationBuilder.AlterDatabase()
                .OldAnnotation("Npgsql:PostgresExtension:hstore", ",,");
        }
    }
}
