using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SW.Bitween.MsSql.Migrations
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
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EndpointProperties",
                table: "BusGateways",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ClusterLeases",
                columns: table => new
                {
                    Id = table.Column<string>(type: "varchar(200)", unicode: false, maxLength: 200, nullable: false),
                    Term = table.Column<long>(type: "bigint", nullable: false),
                    OwnerNode = table.Column<string>(type: "varchar(200)", unicode: false, maxLength: 200, nullable: true),
                    AcquiredOn = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClusterLeases", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DataSources",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    AdapterId = table.Column<string>(type: "varchar(200)", unicode: false, maxLength: 200, nullable: false),
                    Kind = table.Column<int>(type: "int", nullable: false),
                    Properties = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SecretProperties = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Inactive = table.Column<bool>(type: "bit", nullable: false),
                    DeduplicationWindowDays = table.Column<int>(type: "int", nullable: false),
                    LastKnownState = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: true),
                    LastHeartbeatOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastException = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ConsecutiveFailures = table.Column<int>(type: "int", nullable: false),
                    OwnedByNode = table.Column<string>(type: "varchar(200)", unicode: false, maxLength: 200, nullable: true),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ModifiedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DataSources", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "InboundMessages",
                columns: table => new
                {
                    Id = table.Column<string>(type: "varchar(400)", unicode: false, maxLength: 400, nullable: false),
                    DataSourceId = table.Column<int>(type: "int", nullable: false),
                    XchangeId = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    SeenOn = table.Column<DateTime>(type: "datetime2", nullable: false)
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
                });

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
