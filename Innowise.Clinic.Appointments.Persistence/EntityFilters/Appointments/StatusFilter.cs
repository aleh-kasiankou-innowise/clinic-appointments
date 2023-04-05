using System.Linq.Expressions;
using Innowise.Clinic.Appointments.Persistence.Models;
using Innowise.Clinic.Shared.Enums;
using Innowise.Clinic.Shared.Services.FiltrationService.Abstractions;
using Innowise.Clinic.Shared.Services.FiltrationService.Attributes;

namespace Innowise.Clinic.Appointments.Persistence.EntityFilters.Appointments;

[FilterKey("status")]
public class StatusFilter : EntityFilter<Appointment>
{
    public override Expression<Func<Appointment, bool>> ToExpression(string value)
    {
        if (int.TryParse(value, out var status))
        {
            var appointment = Expression.Parameter(typeof(Appointment));
            var tableStatus = Expression.Property(appointment, nameof(Appointment.Status));
            var filterStatus = Expression.Constant(status);
            var equalityCheck = Expression.Equal(tableStatus, filterStatus);
            return Expression.Lambda<Func<Appointment, bool>>(equalityCheck, appointment);
        }

        throw new ApplicationException("The format of status is incorrect. Please use integer.");
    }
}