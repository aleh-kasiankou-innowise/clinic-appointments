using System.Linq.Expressions;
using Innowise.Clinic.Appointments.Exceptions;
using Innowise.Clinic.Appointments.Persistence.Models;
using Innowise.Clinic.Shared.Services.FiltrationService.Abstractions;
using Innowise.Clinic.Shared.Services.FiltrationService.Attributes;

namespace Innowise.Clinic.Appointments.Persistence.EntityFilters.AppointmentResults;

[FilterKey("patient")]
public class PatientFilter : EntityFilter<AppointmentResult>
{
    public override Expression<Func<AppointmentResult, bool>> ToExpression(string value)
    {
        if (Guid.TryParse(value, out var patientId))
        {
            var appointmentResult = Expression.Parameter(typeof(AppointmentResult));
            var appointment = Expression.Property(appointmentResult, nameof(AppointmentResult.Appointment));
            var tablePatientId = Expression.Property(appointment, nameof(Appointment.PatientId));
            var filterPatientId = Expression.Constant(patientId);
            var equalityCheck = Expression.Equal(tablePatientId, filterPatientId);
            return Expression.Lambda<Func<AppointmentResult, bool>>(equalityCheck, appointmentResult);
        }

        throw new InvalidFilterValueFormatException("The format of patient id is incorrect. Please use uuid.");
    }
}