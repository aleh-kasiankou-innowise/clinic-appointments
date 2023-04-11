using System.Linq.Expressions;
using Innowise.Clinic.Appointments.Persistence.Models;
using Innowise.Clinic.Shared.MassTransit.MessageTypes.Events;

namespace Innowise.Clinic.Appointments.Persistence.Repositories.Interfaces;

public interface IDoctorRepository
{
    Task<Doctor?> GetDoctorAsync(Expression<Func<Doctor, bool>> filter);
    Task AddOrUpdateDoctorAsync(DoctorAddedOrUpdatedMessage doctorAddedOrUpdatedMessage);

}