using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace July2025Capstone.Migrations
{
    /// <inheritdoc />
    public partial class ChangeSignatureToString : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SignatureImage",
                table: "Consents");

            migrationBuilder.AddColumn<string>(
                name: "SignatureName",
                table: "Consents",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SignatureName",
                table: "Consents");

            migrationBuilder.AddColumn<byte[]>(
                name: "SignatureImage",
                table: "Consents",
                type: "varbinary(max)",
                nullable: true);
        }
    }
}
