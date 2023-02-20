using Innowise.Clinic.Appointments.Persistence.Models;

namespace Innowise.Clinic.Appointments.Dto;

// Patient has appointments history // dto same as create bu

public abstract class AppointmentFilterBaseDto
{
    public DateTime DayFilter { get; set; } = DateTime.Today;
}

public class AppointmentDoctorFilterDto : AppointmentFilterBaseDto
{
    public Guid DoctorIdFilter { get; set; } // must for a doctor
}

public class AppointmentReceptionistFilterDto : AppointmentFilterBaseDto
{
    public Guid? DoctorIdFilter { get; set; }

    public Guid? ServiceIdFilter { get; set; }

    public AppointmentStatus? StatusFilter { get; set; }

    public Guid? OfficeIdFilter { get; set; }
}