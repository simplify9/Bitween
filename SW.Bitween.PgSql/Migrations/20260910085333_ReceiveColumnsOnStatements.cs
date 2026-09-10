using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SW.Bitween.PgSql.Migrations
{
    /// <inheritdoc />
    public partial class ReceiveColumnsOnStatements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "cursor_column",
                schema: "infolink",
                table: "data_source_statement",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "key_column",
                schema: "infolink",
                table: "data_source_statement",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "cursor_column",
                schema: "infolink",
                table: "data_source_statement");

            migrationBuilder.DropColumn(
                name: "key_column",
                schema: "infolink",
                table: "data_source_statement");
        }
    }
}
