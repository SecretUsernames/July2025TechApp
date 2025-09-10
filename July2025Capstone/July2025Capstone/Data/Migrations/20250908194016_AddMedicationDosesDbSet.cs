using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace July2025Capstone.Migrations
{
    /// <inheritdoc />
    public partial class AddMedicationDosesDbSet : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MedicationDose_Medications_MedicationId",
                table: "MedicationDose");

            migrationBuilder.DropPrimaryKey(
                name: "PK_MedicationDose",
                table: "MedicationDose");

            migrationBuilder.RenameTable(
                name: "MedicationDose",
                newName: "MedicationDoses");

            migrationBuilder.RenameIndex(
                name: "IX_MedicationDose_MedicationId",
                table: "MedicationDoses",
                newName: "IX_MedicationDoses_MedicationId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_MedicationDoses",
                table: "MedicationDoses",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_MedicationDoses_Medications_MedicationId",
                table: "MedicationDoses",
                column: "MedicationId",
                principalTable: "Medications",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MedicationDoses_Medications_MedicationId",
                table: "MedicationDoses");

            migrationBuilder.DropPrimaryKey(
                name: "PK_MedicationDoses",
                table: "MedicationDoses");

            migrationBuilder.RenameTable(
                name: "MedicationDoses",
                newName: "MedicationDose");

            migrationBuilder.RenameIndex(
                name: "IX_MedicationDoses_MedicationId",
                table: "MedicationDose",
                newName: "IX_MedicationDose_MedicationId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_MedicationDose",
                table: "MedicationDose",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_MedicationDose_Medications_MedicationId",
                table: "MedicationDose",
                column: "MedicationId",
                principalTable: "Medications",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
