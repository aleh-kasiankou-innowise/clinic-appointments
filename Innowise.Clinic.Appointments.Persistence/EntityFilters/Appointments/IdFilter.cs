using System.Linq.Expressions;
using Innowise.Clinic.Appointments.Persistence.Models;
using Innowise.Clinic.Shared.Services.FiltrationService.Abstractions;
using Innowise.Clinic.Shared.Services.FiltrationService.Attributes;

namespace Innowise.Clinic.Appointments.Persistence.EntityFilters.Appointments;

[FilterKey("id")]
public class IdFilter : EntityFilter<Appointment>
{
    public override Expression<Func<Appointment, bool>> ToExpression(string value)
    {
        var appointment = Expression.Parameter(typeof(Appointment));
        var tableId = Expression.Property(appointment, nameof(Appointment.AppointmentId));
        var filterId = Expression.Constant(Guid.Parse(value));
        var equalityCheck = Expression.Equal(tableId, filterId);
        return Expression.Lambda<Func<Appointment, bool>>(equalityCheck, appointment);
    }
}