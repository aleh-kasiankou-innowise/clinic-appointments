namespace Innowise.Clinic.Appointments.Exceptions;

public class MissingErrorMessageException : Exception
{
    public MissingErrorMessageException() : base("No fail reason provided")
    {
        
    }
}