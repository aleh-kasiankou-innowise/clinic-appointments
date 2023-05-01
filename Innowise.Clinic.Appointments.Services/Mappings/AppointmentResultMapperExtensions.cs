using Innowise.Clinic.Appointments.Dto;
using Innowise.Clinic.Appointments.Persistence.Models;
using Innowise.Clinic.Shared.MassTransit.MessageTypes.Events;

namespace Innowise.Clinic.Appointments.Services.Mappings;

public static class AppointmentResultMapperExtensions
{
    public static ViewAppointmentResultDto ToFrontendPresentation(this AppointmentResult appointmentResult)
    {
        return new ViewAppointmentResultDto
        {
            AppointmentResultId = appointmentResult.AppointmentResultId,
            AppointmentDate = appointmentResult.Appointment.ReservedTimeSlot.AppointmentStart.Date,
            PatientId = appointmentResult.Appointment.PatientId,
            DoctorId = appointmentResult.Appointment.DoctorId,
            SpecializationId = appointmentResult.Appointment.Doctor.SpecializationId,
            ServiceId = appointmentResult.Appointment.ServiceId,
            Complaints = appointmentResult.Complaints,
            Conclusion = appointmentResult.Conclusion,
            Recommendations = appointmentResult.Recommendations
        };
    }

    public static AppointmentResult ToNewAppointmentResult(this CreateAppointmentResultDto newAppointmentResult)
    {
        return new AppointmentResult
        {
            Complaints = newAppointmentResult.Complaints,
            Conclusion = newAppointmentResult.Conclusion,
            Recommendations = newAppointmentResult.Recommendations,
            AppointmentId = newAppointmentResult.AppointmentId
        };
    }

    public static AppointmentResult ToUpdatedAppointmentResult(this AppointmentResultEditDto appointmentResultEditDto, Guid appointmentResultId)
    {
        return new AppointmentResult
        {
            AppointmentResultId = appointmentResultId,
            Complaints = appointmentResultEditDto.Complaints,
            Conclusion = appointmentResultEditDto.Conclusion,
            Recommendations = appointmentResultEditDto.Recommendations
        };
    }

    public static AppointmentResultNotification ToNotification(this AppointmentResult savedAppointmentResult)
    {
        return new(savedAppointmentResult.AppointmentId,
            savedAppointmentResult.Appointment.PatientId, savedAppointmentResult.Appointment.DoctorId,
            savedAppointmentResult.Appointment.ServiceId,
            savedAppointmentResult.Appointment.ReservedTimeSlot.AppointmentStart, savedAppointmentResult.Complaints,
            savedAppointmentResult.Conclusion, savedAppointmentResult.Recommendations);
    }
}