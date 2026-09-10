using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SW.Bitween.MsSql.Migrations
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
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DataSourceId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "varchar(200)", unicode: false, maxLength: 200, nullable: false),
                    Sql = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    WorkGroupId = table.Column<int>(type: "int", nullable: true),
                    Inactive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ModifiedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
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
                });

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
