using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SW.Bitween.PgSql.Migrations
{
    /// <inheritdoc />
    public partial class RetireInformationType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "retired_on",
                schema: "infolink",
                table: "document",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.UpdateData(
                schema: "infolink",
                table: "document",
                keyColumn: "id",
                keyValue: 10001,
                column: "retired_on",
                value: null);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "retired_on",
                schema: "infolink",
                table: "document");
        }
    }
}
