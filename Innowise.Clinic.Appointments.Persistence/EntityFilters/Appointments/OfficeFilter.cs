using System.Linq.Expressions;
using Innowise.Clinic.Appointments.Persistence.Models;
using Innowise.Clinic.Shared.Services.FiltrationService.Abstractions;
using Innowise.Clinic.Shared.Services.FiltrationService.Attributes;

namespace Innowise.Clinic.Appointments.Persistence.EntityFilters.Appointments;

[FilterKey("office")]
public class OfficeFilter : EntityFilter<Appointment>
{

    public override Expression<Func<Appointment, bool>> ToExpression(string value)
    {
        if (Guid.TryParse(value, out var officeId))
        {
            var appointment = Expression.Parameter(typeof(Appointment));
            var tableDoctor = Expression.Property(appointment, nameof(Appointment.Doctor));
            var tableOfficeId = Expression.Property(tableDoctor, nameof(Doctor.OfficeId));
            var filterOfficeId = Expression.Constant(officeId);
            var equalityCheck = Expression.Equal(tableOfficeId, filterOfficeId);
            return Expression.Lambda<Func<Appointment, bool>>(equalityCheck, appointment);
        }

        throw new ApplicationException("The format of office id is incorrect. Please use uuid.");
    }
}