using FluentMigrator;

namespace Innowise.Clinic.Appointments.Persistence.Migrations;

[Migration(3, "Add PdfLink AppointmentResult table")]
public class AddPdfLinkToAppointmentResult : AutoReversingMigration
{
    public override void Up()
    {
        Alter.Table("AppointmentResult")
            .AddColumn("PdfLink")
            .AsString(300)
            .Nullable();
    }
}