using System.Linq.Expressions;
using Innowise.Clinic.Appointments.Persistence.Models;

namespace Innowise.Clinic.Appointments.Persistence.Repositories.Interfaces;

public interface IAppointmentsRepository
{
    Task<Appointment> GetAppointmentAsync(Guid appointmentId);
    Task<IEnumerable<Appointment>> GetAppointmentsListingAsync(Expression<Func<Appointment, bool>> filter);
    Task<Appointment> CreateAppointmentAsync(Appointment newAppointment);
    Task<Appointment> UpdateAppointmentAsync(Appointment updatedAppointment);
}