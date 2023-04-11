using System.Linq.Expressions;
using Innowise.Clinic.Appointments.Persistence.Models;
using Innowise.Clinic.Shared.Services.FiltrationService.Abstractions;
using Innowise.Clinic.Shared.Services.FiltrationService.Attributes;

namespace Innowise.Clinic.Appointments.Persistence.EntityFilters.Doctors;

[FilterKey("id")]
public class IdFilter : EntityFilter<Doctor>
{
    public override Expression<Func<Doctor, bool>> ToExpression(string id)
    {
        var doctor = Expression.Parameter(typeof(Doctor));
        var tableId = Expression.Property(doctor, nameof(Doctor.DoctorId));
        var filterId = Expression.Constant(Guid.Parse(id));
        var equalityCheck = Expression.Equal(tableId, filterId);
        return Expression.Lambda<Func<Doctor, bool>>(equalityCheck, doctor);
    }
}