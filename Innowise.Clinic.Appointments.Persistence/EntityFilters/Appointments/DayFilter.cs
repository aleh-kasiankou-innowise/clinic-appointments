using System.Linq.Expressions;
using Innowise.Clinic.Appointments.Exceptions;
using Innowise.Clinic.Appointments.Persistence.Models;
using Innowise.Clinic.Shared.Services.FiltrationService.Abstractions;
using Innowise.Clinic.Shared.Services.FiltrationService.Attributes;

namespace Innowise.Clinic.Appointments.Persistence.EntityFilters.Appointments;

[FilterKey("day")]
public class DayFilter : EntityFilter<Appointment>
{
    public override Expression<Func<Appointment, bool>> ToExpression(string value)
    {
        if (DateTime.TryParse(value, out var date))
        {
            var appointment = Expression.Parameter(typeof(Appointment));
            var tableTimeSlot = Expression.Property(appointment, nameof(Appointment.ReservedTimeSlot));
            var timeSlotTableDate = Expression.Property(tableTimeSlot, nameof(ReservedTimeSlot.AppointmentStart));
            var filterDate = Expression.Constant(date);
            var equalityCheck = Expression.Equal(timeSlotTableDate, filterDate);
            return Expression.Lambda<Func<Appointment, bool>>(equalityCheck, appointment);
        }

        throw new InvalidFilterValueFormatException("The date in the filter is invalid.");
    }
}