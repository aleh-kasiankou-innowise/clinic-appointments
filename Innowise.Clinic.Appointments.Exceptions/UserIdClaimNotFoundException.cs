namespace Innowise.Clinic.Appointments.Exceptions;

public class UserIdClaimNotFoundException : ApplicationException
{
    private const string DefaultMessage = "The JWT token doesn't contain user id claim";

    public UserIdClaimNotFoundException() : base(DefaultMessage)
    {
    }
}