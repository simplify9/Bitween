using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SW.Bitween.PgSql.Migrations
{
    /// <inheritdoc />
    public partial class ClusterLeases : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "cluster_lease",
                schema: "infolink",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    term = table.Column<long>(type: "bigint", nullable: false),
                    owner_node = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    acquired_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_cluster_lease", x => x.id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "cluster_lease",
                schema: "infolink");
        }
    }
}
