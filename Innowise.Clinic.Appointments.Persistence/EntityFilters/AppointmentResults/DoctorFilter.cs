using System.Linq.Expressions;
using Innowise.Clinic.Appointments.Exceptions;
using Innowise.Clinic.Appointments.Persistence.Models;
using Innowise.Clinic.Shared.Services.FiltrationService.Abstractions;
using Innowise.Clinic.Shared.Services.FiltrationService.Attributes;

namespace Innowise.Clinic.Appointments.Persistence.EntityFilters.AppointmentResults;

[FilterKey("doctor")]
public class DoctorFilter : EntityFilter<AppointmentResult>
{
    public override Expression<Func<AppointmentResult, bool>> ToExpression(string value)
    {
        if (Guid.TryParse(value, out var doctorId))
        {
            var appointmentResult = Expression.Parameter(typeof(AppointmentResult));
            var appointment = Expression.Property(appointmentResult, nameof(AppointmentResult.Appointment));
            var tableDoctorId = Expression.Property(appointment, nameof(Appointment.DoctorId));
            var filterDoctorId = Expression.Constant(doctorId);
            var equalityCheck = Expression.Equal(tableDoctorId, filterDoctorId);
            return Expression.Lambda<Func<AppointmentResult, bool>>(equalityCheck, appointmentResult);
        }

        throw new InvalidFilterValueFormatException("The format of doctor id is incorrect. Please use uuid.");
    }
}