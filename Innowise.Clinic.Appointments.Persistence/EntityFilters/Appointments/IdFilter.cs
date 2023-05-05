using System.Linq.Expressions;
using Innowise.Clinic.Appointments.Exceptions;
using Innowise.Clinic.Appointments.Persistence.Models;
using Innowise.Clinic.Shared.Services.FiltrationService.Abstractions;
using Innowise.Clinic.Shared.Services.FiltrationService.Attributes;

namespace Innowise.Clinic.Appointments.Persistence.EntityFilters.Appointments;

[FilterKey("id")]
public class IdFilter : EntityFilter<Appointment>
{
    public override Expression<Func<Appointment, bool>> ToExpression(string value)
    {
        if (Guid.TryParse(value, out var filterId))
        {
            var appointment = Expression.Parameter(typeof(Appointment));
            var tableId = Expression.Property(appointment, nameof(Appointment.AppointmentId));
            var filterIdExpression = Expression.Constant(filterId);
            var equalityCheck = Expression.Equal(tableId, filterIdExpression);
            return Expression.Lambda<Func<Appointment, bool>>(equalityCheck, appointment);
        }
        
        throw new InvalidFilterValueFormatException("The format of appointment id is incorrect. Please use uuid.");
    }
}