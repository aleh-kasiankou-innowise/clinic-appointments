using Innowise.Clinic.Appointments.Exceptions;

namespace Innowise.Clinic.Appointments.Services.MassTransitService.MessageTypes;

public abstract record ConsistencyCheckResponse(bool DataIsConsistent)
{
    public abstract string ExceptionMessage { get; }

    public void CheckIfDataIsConsistent()
    {
        if (!DataIsConsistent)
        {
            throw new InconsistentDataException(ExceptionMessage);
        }
    }
};

public record ProfileExistsAndHasRoleRequest(Guid ProfileId, string Role);

public record ProfileExistsAndHasRoleResponse(bool DataIsConsistent) : ConsistencyCheckResponse(DataIsConsistent)
{
    public override string ExceptionMessage => "";
};

public record ServiceExistsAndBelongsToSpecializationRequest(Guid ServiceId, Guid SpecializationId);

public record ServiceExistsAndBelongsToSpecializationResponse(bool DataIsConsistent) : ConsistencyCheckResponse(
    DataIsConsistent)
{
    public override string ExceptionMessage => "The requested service either doesn't exist or belongs to a different specialization.";
}