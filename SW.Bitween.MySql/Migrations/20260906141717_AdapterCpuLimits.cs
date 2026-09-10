using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SW.Bitween.MySql.Migrations
{
    /// <inheritdoc />
    public partial class AdapterCpuLimits : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CpuLimitSamples",
                table: "DataSources",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<double>(
                name: "CpuPercentLimit",
                table: "DataSources",
                type: "double",
                nullable: false,
                defaultValue: 0.0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CpuLimitSamples",
                table: "DataSources");

            migrationBuilder.DropColumn(
                name: "CpuPercentLimit",
                table: "DataSources");
        }
    }
}
