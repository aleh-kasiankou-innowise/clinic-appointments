using System.Linq.Expressions;
using Innowise.Clinic.Appointments.Persistence.Models;

namespace Innowise.Clinic.Appointments.Persistence.Repositories.Interfaces;

public interface IAppointmentResultsRepository
{
    Task<AppointmentResult> GetAppointmentResultAsync(Expression<Func<AppointmentResult, bool>> filter);
    Task<IEnumerable<AppointmentResult>> GetAppointmentsListingAsync(Expression<Func<AppointmentResult, bool>> filter);
    Task<Guid> CreateAppointmentResultAsync(AppointmentResult newAppointmentResult);
    Task UpdateAppointmentResultAsync(AppointmentResult appointmentResult);
}