using System.Linq.Expressions;
using Innowise.Clinic.Appointments.Persistence.Models;
using Innowise.Clinic.Shared.Services.FiltrationService.Abstractions;
using Innowise.Clinic.Shared.Services.FiltrationService.Attributes;

namespace Innowise.Clinic.Appointments.Persistence.EntityFilters.AppointmentResults;

[FilterKey("id")]
public class IdFilter : EntityFilter<AppointmentResult>
{
    public override Expression<Func<AppointmentResult, bool>> ToExpression(string value)
    {
        var appointment = Expression.Parameter(typeof(AppointmentResult));
        var tableId = Expression.Property(appointment, nameof(AppointmentResult.AppointmentResultId));
        var filterId = Expression.Constant(Guid.Parse(value));
        var equalityCheck = Expression.Equal(tableId, filterId);
        return Expression.Lambda<Func<AppointmentResult, bool>>(equalityCheck, appointment);
    }
}