namespace Innowise.Clinic.Appointments.Exceptions;

public class InvalidDtoException : ApplicationException
{
    public InvalidDtoException(string message) : base(message)
    {
    }
}