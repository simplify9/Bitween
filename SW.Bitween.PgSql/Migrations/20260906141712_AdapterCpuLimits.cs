using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SW.Bitween.PgSql.Migrations
{
    /// <inheritdoc />
    public partial class AdapterCpuLimits : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "cpu_limit_samples",
                schema: "infolink",
                table: "data_source",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<double>(
                name: "cpu_percent_limit",
                schema: "infolink",
                table: "data_source",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "cpu_limit_samples",
                schema: "infolink",
                table: "data_source");

            migrationBuilder.DropColumn(
                name: "cpu_percent_limit",
                schema: "infolink",
                table: "data_source");
        }
    }
}
