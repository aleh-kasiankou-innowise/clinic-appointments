namespace Innowise.Clinic.Appointments.Exceptions;

public class NotAllowedException : ApplicationException
{
    public NotAllowedException(string message) : base(message)
    {
        
    }
}