using System.Linq.Expressions;
using Innowise.Clinic.Appointments.Persistence.Models;
using Innowise.Clinic.Shared.Services.FiltrationService.Abstractions;

namespace Innowise.Clinic.Appointments.Persistence.EntityFilters.AppointmentResults;

public class PatientFilter : EntityFilter<AppointmentResult>
{
    public override Expression<Func<AppointmentResult, bool>> ToExpression(string value)
    {
        if (Guid.TryParse(value, out var patientId))
        {
            var appointmentResult = Expression.Parameter(typeof(AppointmentResult));
            // TODO THIS WON'T WORK AT THE MOMENT
            var appointment = Expression.Property(appointmentResult, nameof(AppointmentResult.Appointment));
            var tablePatientId = Expression.Property(appointment, nameof(Appointment.PatientId));
            var filterPatientId = Expression.Constant(patientId);
            var equalityCheck = Expression.Equal(tablePatientId, filterPatientId);
            return Expression.Lambda<Func<AppointmentResult, bool>>(equalityCheck, appointmentResult);
        }

        throw new ApplicationException("The format of doctor id is incorrect. Please use uuid.");
    }
}