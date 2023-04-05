using System.Linq.Expressions;
using Innowise.Clinic.Appointments.Persistence.Models;
using Innowise.Clinic.Shared.Services.FiltrationService.Abstractions;
using Innowise.Clinic.Shared.Services.FiltrationService.Attributes;

namespace Innowise.Clinic.Appointments.Persistence.EntityFilters.Appointments;

[FilterKey("doctor")]
public class DoctorFilter : EntityFilter<Appointment>
{
    public override Expression<Func<Appointment, bool>> ToExpression(string value)
    {
        if (Guid.TryParse(value, out var doctorId))
        {
            var appointment = Expression.Parameter(typeof(Appointment));
            var tableDoctorId = Expression.Property(appointment, nameof(Appointment.DoctorId));
            var filterDoctorId = Expression.Constant(doctorId);
            var equalityCheck = Expression.Equal(tableDoctorId, filterDoctorId);
            return Expression.Lambda<Func<Appointment, bool>>(equalityCheck, appointment);
        }

        throw new ApplicationException("The format of doctor id is incorrect. Please use uuid.");
    }
}