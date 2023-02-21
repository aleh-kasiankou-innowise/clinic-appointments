using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Innowise.Clinic.Appointments.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AppointmentResults",
                columns: table => new
                {
                    AppointmentResultId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Complaints = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Conclusion = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Recommendations = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppointmentResults", x => x.AppointmentResultId);
                });

            migrationBuilder.CreateTable(
                name: "ReservedTimeSlots",
                columns: table => new
                {
                    ReservedTimeSlotId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AppointmentStart = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AppointmentFinish = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReservedTimeSlots", x => x.ReservedTimeSlotId);
                });

            migrationBuilder.CreateTable(
                name: "Appointments",
                columns: table => new
                {
                    AppointmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DoctorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SpecializationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ServiceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OfficeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PatientId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ReservedTimeSlotId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AppointmentResultId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Appointments", x => x.AppointmentId);
                    table.ForeignKey(
                        name: "FK_Appointments_AppointmentResults_AppointmentResultId",
                        column: x => x.AppointmentResultId,
                        principalTable: "AppointmentResults",
                        principalColumn: "AppointmentResultId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Appointments_ReservedTimeSlots_ReservedTimeSlotId",
                        column: x => x.ReservedTimeSlotId,
                        principalTable: "ReservedTimeSlots",
                        principalColumn: "ReservedTimeSlotId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_AppointmentResultId",
                table: "Appointments",
                column: "AppointmentResultId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_ReservedTimeSlotId",
                table: "Appointments",
                column: "ReservedTimeSlotId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Appointments");

            migrationBuilder.DropTable(
                name: "AppointmentResults");

            migrationBuilder.DropTable(
                name: "ReservedTimeSlots");
        }
    }
}
