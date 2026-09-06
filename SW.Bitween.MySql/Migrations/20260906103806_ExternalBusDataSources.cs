using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SW.Bitween.MySql.Migrations
{
    /// <inheritdoc />
    public partial class ExternalBusDataSources : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DataSourceId",
                table: "BusGateways",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Endpoint",
                table: "BusGateways",
                type: "varchar(500)",
                unicode: false,
                maxLength: 500,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "EndpointProperties",
                table: "BusGateways",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ClusterLeases",
                columns: table => new
                {
                    Id = table.Column<string>(type: "varchar(200)", unicode: false, maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Term = table.Column<long>(type: "bigint", nullable: false),
                    OwnerNode = table.Column<string>(type: "varchar(200)", unicode: false, maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AcquiredOn = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClusterLeases", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "DataSources",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AdapterId = table.Column<string>(type: "varchar(200)", unicode: false, maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Kind = table.Column<int>(type: "int", nullable: false),
                    Properties = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SecretProperties = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Inactive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    DeduplicationWindowDays = table.Column<int>(type: "int", nullable: false),
                    LastKnownState = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    LastHeartbeatOn = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    LastException = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ConsecutiveFailures = table.Column<int>(type: "int", nullable: false),
                    OwnedByNode = table.Column<string>(type: "varchar(200)", unicode: false, maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedOn = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CreatedBy = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ModifiedOn = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    ModifiedBy = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DataSources", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "InboundMessages",
                columns: table => new
                {
                    Id = table.Column<string>(type: "varchar(400)", unicode: false, maxLength: 400, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DataSourceId = table.Column<int>(type: "int", nullable: false),
                    XchangeId = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SeenOn = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InboundMessages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InboundMessages_DataSources_DataSourceId",
                        column: x => x.DataSourceId,
                        principalTable: "DataSources",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_BusGateways_DataSourceId",
                table: "BusGateways",
                column: "DataSourceId");

            migrationBuilder.CreateIndex(
                name: "IX_DataSources_Name",
                table: "DataSources",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InboundMessages_DataSourceId",
                table: "InboundMessages",
                column: "DataSourceId");

            migrationBuilder.CreateIndex(
                name: "IX_InboundMessages_SeenOn",
                table: "InboundMessages",
                column: "SeenOn");

            migrationBuilder.AddForeignKey(
                name: "FK_BusGateways_DataSources_DataSourceId",
                table: "BusGateways",
                column: "DataSourceId",
                principalTable: "DataSources",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BusGateways_DataSources_DataSourceId",
                table: "BusGateways");

            migrationBuilder.DropTable(
                name: "ClusterLeases");

            migrationBuilder.DropTable(
                name: "InboundMessages");

            migrationBuilder.DropTable(
                name: "DataSources");

            migrationBuilder.DropIndex(
                name: "IX_BusGateways_DataSourceId",
                table: "BusGateways");

            migrationBuilder.DropColumn(
                name: "DataSourceId",
                table: "BusGateways");

            migrationBuilder.DropColumn(
                name: "Endpoint",
                table: "BusGateways");

            migrationBuilder.DropColumn(
                name: "EndpointProperties",
                table: "BusGateways");
        }
    }
}
