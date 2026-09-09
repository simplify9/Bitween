using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SW.Bitween.PgSql.Migrations
{
    /// <inheritdoc />
    public partial class DatabaseDataSources : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "data_source_id",
                schema: "infolink",
                table: "subscription",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "placement",
                schema: "infolink",
                table: "data_source",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "adapter_state",
                schema: "infolink",
                columns: table => new
                {
                    adapter_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    instance_key = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    value = table.Column<string>(type: "character varying(8000)", maxLength: 8000, nullable: true),
                    updated_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_adapter_state", x => new { x.adapter_id, x.instance_key, x.name });
                });

            migrationBuilder.CreateIndex(
                name: "ix_subscription_data_source_id",
                schema: "infolink",
                table: "subscription",
                column: "data_source_id");

            migrationBuilder.AddForeignKey(
                name: "fk_subscription_data_source_data_source_id",
                schema: "infolink",
                table: "subscription",
                column: "data_source_id",
                principalSchema: "infolink",
                principalTable: "data_source",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_subscription_data_source_data_source_id",
                schema: "infolink",
                table: "subscription");

            migrationBuilder.DropTable(
                name: "adapter_state",
                schema: "infolink");

            migrationBuilder.DropIndex(
                name: "ix_subscription_data_source_id",
                schema: "infolink",
                table: "subscription");

            migrationBuilder.DropColumn(
                name: "data_source_id",
                schema: "infolink",
                table: "subscription");

            migrationBuilder.DropColumn(
                name: "placement",
                schema: "infolink",
                table: "data_source");
        }
    }
}
