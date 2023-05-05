using System.Linq.Expressions;
using Innowise.Clinic.Appointments.Exceptions;
using Innowise.Clinic.Appointments.Persistence.Models;
using Innowise.Clinic.Shared.Services.FiltrationService.Abstractions;
using Innowise.Clinic.Shared.Services.FiltrationService.Attributes;

namespace Innowise.Clinic.Appointments.Persistence.EntityFilters.AppointmentResults;

[FilterKey("appointment")]
public class AppointmentIdFilter : IEntityFilter<AppointmentResult>
{
    public Expression<Func<AppointmentResult, bool>> ToExpression(string value)
    {
        if (Guid.TryParse(value, out var appointmentId))
        {
            var appointmentResult = Expression.Parameter(typeof(AppointmentResult));
            var tableAppointmentId = Expression.Property(appointmentResult, nameof(AppointmentResult.AppointmentId));
            var filterAppointmentId = Expression.Constant(appointmentId);
            var equalityCheck = Expression.Equal(tableAppointmentId, filterAppointmentId);
            return Expression.Lambda<Func<AppointmentResult, bool>>(equalityCheck, appointmentResult);
        }

        throw new InvalidFilterValueFormatException("The format of appointment id is incorrect. Please use uuid.");
    }
}