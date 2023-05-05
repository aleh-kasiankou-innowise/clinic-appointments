using System.Linq.Expressions;
using Innowise.Clinic.Appointments.Exceptions;
using Innowise.Clinic.Appointments.Persistence.Models;
using Innowise.Clinic.Shared.Services.FiltrationService.Abstractions;
using Innowise.Clinic.Shared.Services.FiltrationService.Attributes;

namespace Innowise.Clinic.Appointments.Persistence.EntityFilters.TimeSlots;

[FilterKey("id")]
public class IdFilter : EntityFilter<ReservedTimeSlot>
{
    public override Expression<Func<ReservedTimeSlot, bool>> ToExpression(string value)
    {
        if (Guid.TryParse(value, out var filterId))
        {
            var appointment = Expression.Parameter(typeof(ReservedTimeSlot));
            var tableId = Expression.Property(appointment, nameof(ReservedTimeSlot.ReservedTimeSlotId));
            var filterIdExpression = Expression.Constant(Guid.Parse(value));
            var equalityCheck = Expression.Equal(tableId, filterIdExpression);
            return Expression.Lambda<Func<ReservedTimeSlot, bool>>(equalityCheck, appointment);
        }
        throw new InvalidFilterValueFormatException("The format of timeslot id is incorrect. Please use uuid.");
    }
}