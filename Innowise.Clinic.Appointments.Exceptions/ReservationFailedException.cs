namespace Innowise.Clinic.Appointments.Exceptions;

public class ReservationFailedException : ApplicationException
{
    public ReservationFailedException(string message) : base(message)
    {
        
    }
}