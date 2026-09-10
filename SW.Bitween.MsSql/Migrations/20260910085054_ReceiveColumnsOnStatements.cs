using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SW.Bitween.MsSql.Migrations
{
    /// <inheritdoc />
    public partial class ReceiveColumnsOnStatements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CursorColumn",
                table: "DataSourceStatements",
                type: "varchar(128)",
                unicode: false,
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "KeyColumn",
                table: "DataSourceStatements",
                type: "varchar(128)",
                unicode: false,
                maxLength: 128,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CursorColumn",
                table: "DataSourceStatements");

            migrationBuilder.DropColumn(
                name: "KeyColumn",
                table: "DataSourceStatements");
        }
    }
}
