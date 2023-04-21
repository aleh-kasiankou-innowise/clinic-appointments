using FluentMigrator;

namespace Innowise.Clinic.Appointments.Persistence.Migrations;

[Migration(1, "Add AppointmentId FK to AppointmentResult table")]
public class AddAppointmentResultAppointmentFk : AutoReversingMigration
{
    public override void Up()
    {
        Alter.Table("AppointmentResult")
            .AddColumn("AppointmentId")
            .AsGuid()
            .ForeignKey("Appointment","AppointmentId");
    }
}