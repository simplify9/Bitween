using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SW.Bitween.PgSql.Migrations
{
    /// <inheritdoc />
    public partial class AdapterMemoryLimits : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "hard_memory_limit_mb",
                schema: "infolink",
                table: "data_source",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "soft_memory_limit_mb",
                schema: "infolink",
                table: "data_source",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "hard_memory_limit_mb",
                schema: "infolink",
                table: "data_source");

            migrationBuilder.DropColumn(
                name: "soft_memory_limit_mb",
                schema: "infolink",
                table: "data_source");
        }
    }
}
