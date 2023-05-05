using System.Linq.Expressions;
using Innowise.Clinic.Appointments.Persistence.Models;

namespace Innowise.Clinic.Appointments.Persistence.Repositories.Interfaces;

public interface IAppointmentsRepository
{
    Task<Appointment> GetAppointmentAsync(Expression<Func<Appointment, bool>> filter);
    Task<IEnumerable<Appointment>> GetAppointmentsListingAsync(Expression<Func<Appointment, bool>> filter);
    Task<Guid> CreateAppointmentAsync(Appointment newAppointment);
    Task UpdateAppointmentAsync(Appointment updatedAppointment);
}