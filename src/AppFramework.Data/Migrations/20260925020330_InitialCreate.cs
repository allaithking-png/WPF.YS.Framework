using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AppFramework.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LocalRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    SourceKey = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    ItemKey = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    ClrType = table.Column<string>(type: "TEXT", maxLength: 512, nullable: false),
                    Payload = table.Column<string>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LocalRecords", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Outbox",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    SourceKey = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    ItemKey = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    Kind = table.Column<int>(type: "INTEGER", nullable: false),
                    Payload = table.Column<string>(type: "TEXT", nullable: true),
                    ClrType = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Attempts = table.Column<int>(type: "INTEGER", nullable: false),
                    LastError = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Outbox", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LocalRecords_SourceKey_ItemKey",
                table: "LocalRecords",
                columns: new[] { "SourceKey", "ItemKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LocalRecords_UpdatedAt",
                table: "LocalRecords",
                column: "UpdatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Outbox_CreatedAt",
                table: "Outbox",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Outbox_SourceKey",
                table: "Outbox",
                column: "SourceKey");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LocalRecords");

            migrationBuilder.DropTable(
                name: "Outbox");
        }
    }
}
