using System;
using FluentMigrator;

#nullable disable

namespace Innowise.Clinic.Appointments.Persistence.Migrations
{
    /// <inheritdoc />
    [Migration(0, "Creating Doctor, ReservedTimeSlot, AppointmentResult and Appointment tables")]
    public class Init : AutoReversingMigration
    {
        /// <inheritdoc />
        public override void Up()
        {
            Create.Table("Doctor")
                .WithColumn("DoctorId").AsGuid().PrimaryKey()
                .WithColumn("SpecializationId").AsGuid()
                .WithColumn("OfficeId").AsGuid();
            
            Create.Table("ReservedTimeSlot")
                .WithColumn("ReservedTimeSlotId").AsGuid().PrimaryKey()
                .WithColumn("AppointmentStart").AsDateTime2()
                .WithColumn("AppointmentFinish").AsDateTime2();
            
            Create.Table("AppointmentResult")
                .WithColumn("AppointmentResultId").AsGuid().PrimaryKey()
                .WithColumn("Complaints").AsString(500)
                .WithColumn("Conclusion").AsString(500)
                .WithColumn("Recommendations").AsString(500);

            Create.Table("Appointment")
                .WithColumn("AppointmentId").AsGuid().PrimaryKey()
                .WithColumn("DoctorId").AsGuid().ForeignKey("Doctor", "DoctorId")
                .WithColumn("ServiceId").AsGuid()
                .WithColumn("PatientId").AsGuid()
                .WithColumn("Status").AsInt32()
                .WithColumn("ReservedTimeSlotId").AsGuid()
                    .ForeignKey("ReservedTimeSlot", "ReservedTimeSlotId")
                .WithColumn("AppointmentResultId").AsGuid().Nullable()
                    .ForeignKey("AppointmentResult", "AppointmentResultId");
        }
    }
}