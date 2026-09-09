using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SW.Bitween.MsSql.Migrations
{
    /// <inheritdoc />
    public partial class DatabaseDataSources : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DataSourceId",
                table: "Subscriptions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Placement",
                table: "DataSources",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "AdapterStates",
                columns: table => new
                {
                    AdapterId = table.Column<string>(type: "varchar(200)", unicode: false, maxLength: 200, nullable: false),
                    InstanceKey = table.Column<string>(type: "varchar(200)", unicode: false, maxLength: 200, nullable: false),
                    Name = table.Column<string>(type: "varchar(200)", unicode: false, maxLength: 200, nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", maxLength: 8000, nullable: true),
                    UpdatedOn = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdapterStates", x => new { x.AdapterId, x.InstanceKey, x.Name });
                });

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_DataSourceId",
                table: "Subscriptions",
                column: "DataSourceId");

            migrationBuilder.AddForeignKey(
                name: "FK_Subscriptions_DataSources_DataSourceId",
                table: "Subscriptions",
                column: "DataSourceId",
                principalTable: "DataSources",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Subscriptions_DataSources_DataSourceId",
                table: "Subscriptions");

            migrationBuilder.DropTable(
                name: "AdapterStates");

            migrationBuilder.DropIndex(
                name: "IX_Subscriptions_DataSourceId",
                table: "Subscriptions");

            migrationBuilder.DropColumn(
                name: "DataSourceId",
                table: "Subscriptions");

            migrationBuilder.DropColumn(
                name: "Placement",
                table: "DataSources");
        }
    }
}
