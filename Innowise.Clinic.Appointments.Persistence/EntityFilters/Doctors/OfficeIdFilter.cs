using System.Linq.Expressions;
using Innowise.Clinic.Appointments.Persistence.Models;
using Innowise.Clinic.Shared.Services.FiltrationService.Abstractions;
using Innowise.Clinic.Shared.Services.FiltrationService.Attributes;

namespace Innowise.Clinic.Appointments.Persistence.EntityFilters.Doctors;

[FilterKey("office")]
public class OfficeIdFilter : EntityFilter<Doctor>
{
    public override Expression<Func<Doctor, bool>> ToExpression(string value)
    {
        if (Guid.TryParse(value, out var officeId))
        {
            var appointment = Expression.Parameter(typeof(Doctor));
            var tableOfficeId = Expression.Property(appointment, nameof(Doctor.OfficeId));
            var filterOfficeId = Expression.Constant(officeId);
            var equalityCheck = Expression.Equal(tableOfficeId, filterOfficeId);
            return Expression.Lambda<Func<Doctor, bool>>(equalityCheck, appointment);
        }

        throw new ApplicationException("The format of office id is incorrect. Please use uuid.");
    }
}