using Innowise.Clinic.Appointments.Exceptions;
using Innowise.Clinic.Appointments.Persistence.Models;
using Innowise.Clinic.Appointments.Services.TimeSlotsService.Interfaces;
using Innowise.Clinic.Shared.MassTransit.MessageTypes.Requests;
using MassTransit;

namespace Innowise.Clinic.Appointments.Services.TimeSlotsService.Implementations;

public class TimeSlotService : ITimeSlotsService
{
    private readonly IRequestClient<TimeSlotReservationRequest> _timeslotReservationClient;
    private readonly IRequestClient<UpdateAppointmentTimeslotRequest> _timeslotUpdateClient;

    public TimeSlotService(IRequestClient<TimeSlotReservationRequest> timeslotReservationClient,
        IRequestClient<UpdateAppointmentTimeslotRequest> timeslotUpdateClient)
    {
        _timeslotReservationClient = timeslotReservationClient;
        _timeslotUpdateClient = timeslotUpdateClient;
    }

    public async Task<ReservedTimeSlot> TryReserveTimeSlot(Guid doctorId, DateTime appointmentStart,
        DateTime appointmentFinish)
    {
        var timeSlotReservationResult = await _timeslotReservationClient
            .GetResponse<TimeSlotReservationResponse>(new(
                doctorId,
                appointmentStart,
                appointmentFinish)
            );

        if (timeSlotReservationResult.Message.ReservedTimeSlotId is null)
        {
            throw new ReservationFailedException(timeSlotReservationResult.Message.FailReason ??
                                                 throw new MissingErrorMessageException());
        }

        return new ReservedTimeSlot
        {
            ReservedTimeSlotId = (Guid)timeSlotReservationResult.Message.ReservedTimeSlotId,
            AppointmentStart = appointmentStart,
            AppointmentFinish = appointmentFinish
        };
    }

    public async Task<ReservedTimeSlot> TryRebookTimeSlot(Guid timeSlotId, Guid doctorId, DateTime appointmentStart,
        DateTime appointmentFinish)
    {
        var timeslotUpdateResponse = await _timeslotUpdateClient.GetResponse<UpdateAppointmentTimeslotResponse>(
            new(timeSlotId, new(
                doctorId,
                appointmentStart,
                appointmentFinish)));

        if (!timeslotUpdateResponse.Message.IsSuccessful)
        {
            var message = timeslotUpdateResponse.Message.FailReason ??
                          throw new MissingErrorMessageException();
            throw new ReservationFailedException(message);
        }

        return new ReservedTimeSlot
        {
            ReservedTimeSlotId = timeSlotId,
            AppointmentStart = appointmentStart,
            AppointmentFinish = appointmentFinish
        };
    }
}