using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NoticeSaaS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddClientSyncAndPortalCredentials : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CaPan",
                table: "Clients",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastSyncAtUtc",
                table: "Clients",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "NextSyncAtUtc",
                table: "Clients",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PortalUsername",
                table: "Clients",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SyncFrequency",
                table: "Clients",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "PortalCredentials",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClientId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Username = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PasswordProtected = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PortalCredentials", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PortalCredentials_Clients_ClientId",
                        column: x => x.ClientId,
                        principalTable: "Clients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PortalCredentials_ClientId",
                table: "PortalCredentials",
                column: "ClientId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PortalCredentials");

            migrationBuilder.DropColumn(
                name: "CaPan",
                table: "Clients");

            migrationBuilder.DropColumn(
                name: "LastSyncAtUtc",
                table: "Clients");

            migrationBuilder.DropColumn(
                name: "NextSyncAtUtc",
                table: "Clients");

            migrationBuilder.DropColumn(
                name: "PortalUsername",
                table: "Clients");

            migrationBuilder.DropColumn(
                name: "SyncFrequency",
                table: "Clients");
        }
    }
}
