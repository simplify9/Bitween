using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SW.Bitween.MsSql.Migrations
{
    /// <inheritdoc />
    public partial class RetireInformationType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "RetiredOn",
                table: "Documents",
                type: "datetime2",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Documents",
                keyColumn: "Id",
                keyValue: 10001,
                column: "RetiredOn",
                value: null);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RetiredOn",
                table: "Documents");
        }
    }
}
