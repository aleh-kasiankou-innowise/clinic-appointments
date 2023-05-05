using System.Linq.Expressions;
using Innowise.Clinic.Appointments.Exceptions;
using Innowise.Clinic.Appointments.Persistence.Models;
using Innowise.Clinic.Shared.Services.FiltrationService.Abstractions;
using Innowise.Clinic.Shared.Services.FiltrationService.Attributes;

namespace Innowise.Clinic.Appointments.Persistence.EntityFilters.Doctors;

[FilterKey("id")]
public class IdFilter : EntityFilter<Doctor>
{
    public override Expression<Func<Doctor, bool>> ToExpression(string value)
    {
        if (Guid.TryParse(value, out var filterId))
        {
            var doctor = Expression.Parameter(typeof(Doctor));
            var tableId = Expression.Property(doctor, nameof(Doctor.DoctorId));
            var filterIdExpression = Expression.Constant(filterId);
            var equalityCheck = Expression.Equal(tableId, filterIdExpression);
            return Expression.Lambda<Func<Doctor, bool>>(equalityCheck, doctor);
        }
        
        throw new InvalidFilterValueFormatException("The format of doctor id is incorrect. Please use uuid.");
    }
}