using System.Linq.Expressions;
using Innowise.Clinic.Appointments.Exceptions;
using Innowise.Clinic.Appointments.Persistence.Models;
using Innowise.Clinic.Shared.Services.FiltrationService.Abstractions;
using Innowise.Clinic.Shared.Services.FiltrationService.Attributes;

namespace Innowise.Clinic.Appointments.Persistence.EntityFilters.Appointments;

[FilterKey("service")]
public class ServiceFilter : EntityFilter<Appointment>
{
    public override Expression<Func<Appointment, bool>> ToExpression(string value)
    {
        if (Guid.TryParse(value, out var serviceId))
        {
            var appointment = Expression.Parameter(typeof(Appointment));
            var tableServiceId = Expression.Property(appointment, nameof(Appointment.ServiceId));
            var filterServiceId = Expression.Constant(serviceId);
            var equalityCheck = Expression.Equal(tableServiceId, filterServiceId);
            return Expression.Lambda<Func<Appointment, bool>>(equalityCheck, appointment);
        }

        throw new InvalidFilterValueFormatException("The format of service id is incorrect. Please use uuid.");
    }
}