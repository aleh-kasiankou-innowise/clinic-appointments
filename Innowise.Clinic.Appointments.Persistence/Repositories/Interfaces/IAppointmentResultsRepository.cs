using System.Linq.Expressions;
using Innowise.Clinic.Appointments.Persistence.Models;

namespace Innowise.Clinic.Appointments.Persistence.Repositories.Interfaces;

public interface IAppointmentResultsRepository
{
    Task<AppointmentResult> GetAppointmentResult(Expression<Func<AppointmentResult, bool>> filter);
    Task<AppointmentResult> CreateAppointmentResult(AppointmentResult newAppointmentResult);
    Task<AppointmentResult> UpdateAppointmentResult(AppointmentResult appointmentResult);
}