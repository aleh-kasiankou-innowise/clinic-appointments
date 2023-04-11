using System.Linq.Expressions;
using Innowise.Clinic.Appointments.Persistence.Models;
using Innowise.Clinic.Shared.Services.FiltrationService.Abstractions;
using Innowise.Clinic.Shared.Services.FiltrationService.Attributes;

namespace Innowise.Clinic.Appointments.Persistence.EntityFilters.Doctors;

[FilterKey("specialization")]
public class SpecializationIdFilter : EntityFilter<Doctor>
{
    public override Expression<Func<Doctor, bool>> ToExpression(string value)
    {
        if (Guid.TryParse(value, out var specializationId))
        {
            var appointment = Expression.Parameter(typeof(Doctor));
            var tableSpecializationId = Expression.Property(appointment, nameof(Doctor.SpecializationId));
            var filterSpecializationId = Expression.Constant(specializationId);
            var equalityCheck = Expression.Equal(tableSpecializationId, filterSpecializationId);
            return Expression.Lambda<Func<Doctor, bool>>(equalityCheck, appointment);
        }

        throw new ApplicationException("The format of specialization id is incorrect. Please use uuid.");
    }
}