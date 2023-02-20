using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Innowise.Clinic.Appointments.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class NullableAppoinmentResult : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Appointments_AppointmentResultId",
                table: "Appointments");

            migrationBuilder.AlterColumn<Guid>(
                name: "AppointmentResultId",
                table: "Appointments",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_AppointmentResultId",
                table: "Appointments",
                column: "AppointmentResultId",
                unique: true,
                filter: "[AppointmentResultId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Appointments_AppointmentResultId",
                table: "Appointments");

            migrationBuilder.AlterColumn<Guid>(
                name: "AppointmentResultId",
                table: "Appointments",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_AppointmentResultId",
                table: "Appointments",
                column: "AppointmentResultId",
                unique: true);
        }
    }
}
