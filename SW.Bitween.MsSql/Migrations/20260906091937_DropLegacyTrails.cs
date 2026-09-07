using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SW.Bitween.MsSql.Migrations
{
    /// <inheritdoc />
    public partial class DropLegacyTrails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DocumentTrail");

            migrationBuilder.DropTable(
                name: "SubscriptionTrail");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DocumentTrail",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DocumentId = table.Column<int>(type: "int", nullable: false),
                    Code = table.Column<int>(type: "int", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    StateAfter = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StateBefore = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentTrail", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DocumentTrail_Documents_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "Documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SubscriptionTrail",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    SubscriptionId = table.Column<int>(type: "int", nullable: false),
                    Code = table.Column<int>(type: "int", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    StateAfter = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StateBefore = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubscriptionTrail", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SubscriptionTrail_Subscriptions_SubscriptionId",
                        column: x => x.SubscriptionId,
                        principalTable: "Subscriptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DocumentTrail_CreatedOn",
                table: "DocumentTrail",
                column: "CreatedOn");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentTrail_DocumentId",
                table: "DocumentTrail",
                column: "DocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionTrail_CreatedOn",
                table: "SubscriptionTrail",
                column: "CreatedOn");

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionTrail_SubscriptionId",
                table: "SubscriptionTrail",
                column: "SubscriptionId");
        }
    }
}
