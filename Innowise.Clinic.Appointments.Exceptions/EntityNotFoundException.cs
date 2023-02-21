namespace Innowise.Clinic.Appointments.Exceptions;

public class EntityNotFoundException : ApplicationException
{
    public EntityNotFoundException(string message) : base(message)
    {
        
    }
}