using Innowise.Clinic.Appointments.Dto;
using Innowise.Clinic.Appointments.Persistence.Models;
using Innowise.Clinic.Shared.Enums;

namespace Innowise.Clinic.Appointments.Services.Mappings;

public static class AppointmentMapperExtensions
{
    public static IEnumerable<ViewAppointmentHistoryDto> ToAppointmentHistory(this IEnumerable<Appointment> appointments)
    {
        return appointments.Select(x =>
            new ViewAppointmentHistoryDto
            {
                AppointmentId = x.AppointmentId,
                AppointmentStart = x.ReservedTimeSlot.AppointmentStart,
                AppointmentFinish = x.ReservedTimeSlot.AppointmentFinish,
                DoctorId = x.DoctorId,
                PatientId = x.PatientId,
                ServiceId = x.ServiceId,
            });
    }
    
    public static IEnumerable<AppointmentInfoDto> ToDoctorAppointmentListing(this IEnumerable<Appointment> appointments)
    {
        return appointments.Select(x => new AppointmentInfoDto
        {
            AppointmentId = x.AppointmentId,
            AppointmentStart = x.ReservedTimeSlot.AppointmentStart,
            AppointmentFinish = x.ReservedTimeSlot.AppointmentFinish,
            AppointmentStatus = x.Status,
            PatientId = x.PatientId,
            ServiceId = x.ServiceId,
        });
    }
    
    public static IEnumerable<AppointmentInfoDto> ToAppointmentListing(this IEnumerable<Appointment> appointments)
    {
        return appointments.Select(x => new AppointmentInfoDto
        {
            AppointmentId = x.AppointmentId,
            AppointmentStart = x.ReservedTimeSlot.AppointmentStart,
            AppointmentFinish = x.ReservedTimeSlot.AppointmentFinish,
            AppointmentStatus = x.Status,
            PatientId = x.PatientId,
            ServiceId = x.ServiceId
        });
    }

    public static Appointment ToNewAppointment(this CreateAppointmentDto newAppointmentDto, ReservedTimeSlot timeSlot)
    {
        return new Appointment
        {
            DoctorId = newAppointmentDto.DoctorId,
            ServiceId = newAppointmentDto.ServiceId,
            PatientId = newAppointmentDto.PatientId,
            Status = AppointmentStatus.Created,
            ReservedTimeSlot = timeSlot,
        };
    }

    public static AppointmentEditTimeAndStatusDto ToCompleteUpdateDto(this AppointmentEditTimeDto appointmentEditTimeDto, Appointment savedAppointment)
    {
        return new(
            savedAppointment.AppointmentId,
            appointmentEditTimeDto.DoctorId,
            appointmentEditTimeDto.AppointmentStart,
            appointmentEditTimeDto.AppointmentEnd,
            savedAppointment.Status
        );
    }
}