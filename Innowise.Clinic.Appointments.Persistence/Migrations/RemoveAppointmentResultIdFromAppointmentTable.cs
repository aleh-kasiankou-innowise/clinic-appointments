using FluentMigrator;

namespace Innowise.Clinic.Appointments.Persistence.Migrations;

[Migration(2, "Remove AppointmentResultId column with fk to AppointmentResult table")]
public class RemoveAppointmentResultIdFromAppointmentTable : Migration
{
    public override void Up()
    {
        Delete.Column("AppointmentResultId").FromTable("Appointment");
    }

    public override void Down()
    {
        Alter.Table("Appointment").AddColumn("AppointmentResultId").AsGuid()
            .ForeignKey("AppointmentResult", "AppointmentResult", "AppointmentResultId");
    }
}