using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SW.Bitween.MySql.Migrations
{
    /// <inheritdoc />
    public partial class DataSourceStatements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DataSourceStatements",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    DataSourceId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "varchar(200)", unicode: false, maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Sql = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    WorkGroupId = table.Column<int>(type: "int", nullable: true),
                    Inactive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CreatedBy = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ModifiedOn = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    ModifiedBy = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DataSourceStatements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DataSourceStatements_DataSources_DataSourceId",
                        column: x => x.DataSourceId,
                        principalTable: "DataSources",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DataSourceStatements_WorkGroup_WorkGroupId",
                        column: x => x.WorkGroupId,
                        principalTable: "WorkGroup",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_DataSourceStatements_DataSourceId_Name",
                table: "DataSourceStatements",
                columns: new[] { "DataSourceId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DataSourceStatements_WorkGroupId",
                table: "DataSourceStatements",
                column: "WorkGroupId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DataSourceStatements");
        }
    }
}
