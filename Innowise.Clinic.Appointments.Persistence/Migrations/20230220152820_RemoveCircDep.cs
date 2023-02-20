using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Innowise.Clinic.Appointments.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RemoveCircDep : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ReservedTimeSlots_Appointments_AppointmentId",
                table: "ReservedTimeSlots");

            migrationBuilder.DropIndex(
                name: "IX_ReservedTimeSlots_AppointmentId",
                table: "ReservedTimeSlots");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_ReservedTimeSlots_AppointmentId",
                table: "ReservedTimeSlots",
                column: "AppointmentId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ReservedTimeSlots_Appointments_AppointmentId",
                table: "ReservedTimeSlots",
                column: "AppointmentId",
                principalTable: "Appointments",
                principalColumn: "AppointmentId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
