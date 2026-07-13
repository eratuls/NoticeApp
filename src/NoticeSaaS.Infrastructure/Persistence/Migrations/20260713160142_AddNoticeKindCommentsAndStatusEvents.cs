using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NoticeSaaS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddNoticeKindCommentsAndStatusEvents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Notices_ClientId",
                table: "Notices");

            migrationBuilder.AddColumn<string>(
                name: "Kind",
                table: "Notices",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "Notice");

            migrationBuilder.AddColumn<DateOnly>(
                name: "ResponseSubmittedDate",
                table: "Notices",
                type: "date",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "NoticeComments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NoticeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AuthorUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Body = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NoticeComments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NoticeComments_Notices_NoticeId",
                        column: x => x.NoticeId,
                        principalTable: "Notices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_NoticeComments_Users_AuthorUserId",
                        column: x => x.AuthorUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "NoticeStatusEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NoticeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FromStatus = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    ToStatus = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    ChangedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NoticeStatusEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NoticeStatusEvents_Notices_NoticeId",
                        column: x => x.NoticeId,
                        principalTable: "Notices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Notices_ClientId_Kind",
                table: "Notices",
                columns: new[] { "ClientId", "Kind" });

            migrationBuilder.CreateIndex(
                name: "IX_NoticeComments_AuthorUserId",
                table: "NoticeComments",
                column: "AuthorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_NoticeComments_NoticeId",
                table: "NoticeComments",
                column: "NoticeId");

            migrationBuilder.CreateIndex(
                name: "IX_NoticeStatusEvents_NoticeId",
                table: "NoticeStatusEvents",
                column: "NoticeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NoticeComments");

            migrationBuilder.DropTable(
                name: "NoticeStatusEvents");

            migrationBuilder.DropIndex(
                name: "IX_Notices_ClientId_Kind",
                table: "Notices");

            migrationBuilder.DropColumn(
                name: "Kind",
                table: "Notices");

            migrationBuilder.DropColumn(
                name: "ResponseSubmittedDate",
                table: "Notices");

            migrationBuilder.CreateIndex(
                name: "IX_Notices_ClientId",
                table: "Notices",
                column: "ClientId");
        }
    }
}
