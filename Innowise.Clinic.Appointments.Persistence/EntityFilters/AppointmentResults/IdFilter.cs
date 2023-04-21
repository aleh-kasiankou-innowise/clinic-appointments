using System.Linq.Expressions;
using Innowise.Clinic.Appointments.Exceptions;
using Innowise.Clinic.Appointments.Persistence.Models;
using Innowise.Clinic.Shared.Services.FiltrationService.Abstractions;
using Innowise.Clinic.Shared.Services.FiltrationService.Attributes;

namespace Innowise.Clinic.Appointments.Persistence.EntityFilters.AppointmentResults;

[FilterKey("id")]
public class IdFilter : EntityFilter<AppointmentResult>
{
    public override Expression<Func<AppointmentResult, bool>> ToExpression(string value)
    {
        if (Guid.TryParse(value, out var filterId))
        {
            var appointment = Expression.Parameter(typeof(AppointmentResult));
            var tableId = Expression.Property(appointment, nameof(AppointmentResult.AppointmentResultId));
            var filterIdExpression = Expression.Constant(filterId);
            var equalityCheck = Expression.Equal(tableId, filterIdExpression);
            return Expression.Lambda<Func<AppointmentResult, bool>>(equalityCheck, appointment);
        }

        throw new InvalidFilterValueFormatException("The format of appointment result id is incorrect. Please use uuid.");
    }
}