using System.Linq.Expressions;
using Innowise.Clinic.Appointments.Exceptions;
using Innowise.Clinic.Appointments.Persistence.Models;
using Innowise.Clinic.Shared.Services.FiltrationService.Abstractions;
using Innowise.Clinic.Shared.Services.FiltrationService.Attributes;

namespace Innowise.Clinic.Appointments.Persistence.EntityFilters.Appointments;

[FilterKey("patient")]
public class PatientFilter : IEntityFilter<Appointment>
{
    public Expression<Func<Appointment, bool>> ToExpression(string value)
    {
        if (Guid.TryParse(value, out var patientId))
        {
            var appointment = Expression.Parameter(typeof(Appointment));
            var tablePatientId = Expression.Property(appointment, nameof(Appointment.PatientId));
            var filterPatientId = Expression.Constant(patientId);
            var equalityCheck = Expression.Equal(tablePatientId, filterPatientId);
            return Expression.Lambda<Func<Appointment, bool>>(equalityCheck, appointment);
        }

        throw new InvalidFilterValueFormatException("The format of patient id is incorrect. Please use uuid.");
    }
}