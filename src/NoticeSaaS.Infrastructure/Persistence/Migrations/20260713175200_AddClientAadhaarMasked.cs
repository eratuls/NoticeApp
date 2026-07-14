using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NoticeSaaS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddClientAadhaarMasked : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AadhaarMasked",
                table: "Clients",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AadhaarMasked",
                table: "Clients");
        }
    }
}
