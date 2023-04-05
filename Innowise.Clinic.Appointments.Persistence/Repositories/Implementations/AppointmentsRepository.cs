using System.Linq.Expressions;
using System.Text;
using Dapper;
using Innowise.Clinic.Appointments.Persistence.EntityFilters.Appointments;
using Innowise.Clinic.Appointments.Persistence.Models;
using Innowise.Clinic.Appointments.Persistence.Repositories.Interfaces;
using Npgsql;

namespace Innowise.Clinic.Appointments.Persistence.Repositories.Implementations;

public class AppointmentsRepository : IAppointmentsRepository
{
    private readonly string _selectStatement;
    private readonly string _insertAppointmentStatementTemplate;
    private readonly string _updateAppointmentStatementTemplate;

    private readonly NpgsqlDataSource _dataSource;
    private readonly SqlRepresentation<Appointment> _appointmentSqlRepresentation;
    private readonly SqlRepresentation<ReservedTimeSlot> _timeSlotTableSqlRepresentation;
    private readonly ITimeSlotRepository _timeSlotRepository;

    public AppointmentsRepository(NpgsqlDataSource dataSource,
        SqlRepresentation<Appointment> appointmentTable,
        SqlRepresentation<ReservedTimeSlot> timeSlotTableSqlRepresentation, ITimeSlotRepository timeSlotRepository)
    {
        _dataSource = dataSource;
        _appointmentSqlRepresentation = appointmentTable;
        _timeSlotTableSqlRepresentation = timeSlotTableSqlRepresentation;
        _timeSlotRepository = timeSlotRepository;

        _selectStatement =
            $"SELECT * FROM {_appointmentSqlRepresentation} " +
            $"INNER JOIN {_timeSlotTableSqlRepresentation} " +
            $"on {_appointmentSqlRepresentation.Property(x => x.ReservedTimeSlotId)} = " +
            $"{_timeSlotTableSqlRepresentation.Property(x => x.ReservedTimeSlotId)} " +
            $"WHERE [FILTER];";

        var insertAppointmentStatementTemplate =
            new StringBuilder($"INSERT INTO {_appointmentSqlRepresentation}([FIELDS]) " +
                              $"VALUES( gen_random_uuid() ,[VALUES]) " +
                              $"RETURNING {_appointmentSqlRepresentation.Property(x => x.AppointmentId)}");

        var updateAppointmentStatementTemplate = new StringBuilder(
            $"UPDATE {_appointmentSqlRepresentation} " +
            $"SET [UPDATEMAPPINGS] " +
            $"WHERE [FILTER];");


        _insertAppointmentStatementTemplate =
            _appointmentSqlRepresentation.CompleteInsertStatement(insertAppointmentStatementTemplate);
        _updateAppointmentStatementTemplate =
            _appointmentSqlRepresentation.CompleteUpdateStatement(updateAppointmentStatementTemplate);
    }

    public async Task<Appointment> GetAppointmentAsync(Expression<Func<Appointment, bool>> filter)
    {
        var sqlWithParams =
            _appointmentSqlRepresentation.ApplyFilter(_selectStatement, filter);
        await using var connection = await _dataSource.OpenConnectionAsync();
        return await connection.QuerySingleAsync<Appointment>(sqlWithParams.Sql
            , sqlWithParams.Parameters);
    }

    public async Task<IEnumerable<Appointment>> GetAppointmentsListingAsync(Expression<Func<Appointment, bool>> filter)
    {
        var sqlWithParams = _appointmentSqlRepresentation.ApplyFilter(_selectStatement, filter);
        await using var connection = await _dataSource.OpenConnectionAsync();
        return await connection.QueryAsync<Appointment, ReservedTimeSlot, Appointment>(sqlWithParams.Sql
            , (appointment, timeslot) =>
            {
                appointment.ReservedTimeSlot = timeslot;
                return appointment;
            }, sqlWithParams.Parameters,
            splitOn: _timeSlotTableSqlRepresentation.Property(x => x.ReservedTimeSlotId, true));
    }

    public async Task<Guid> CreateAppointmentAsync(Appointment newAppointment)
    {
        var appointmentSqlParams = _appointmentSqlRepresentation.MapPropertiesToSqlParameters(newAppointment);
        await using var connection = await _dataSource.OpenConnectionAsync();
        var transaction = await connection.BeginTransactionAsync();
        var reservedTimeSlotId = await _timeSlotRepository.ReserveTimeSlot(connection, newAppointment.ReservedTimeSlot);
        newAppointment.ReservedTimeSlotId = reservedTimeSlotId;
        var id = await connection.QueryFirstAsync<Guid>(_insertAppointmentStatementTemplate, appointmentSqlParams);
        await transaction.CommitAsync();
        return id;
    }

    public async Task UpdateAppointmentAsync(Appointment updatedAppointment)
    {
        var savedAppointment =
            await GetAppointmentAsync(new IdFilter().ToExpression(updatedAppointment.AppointmentId.ToString()));
        var isTimeSlotUpdateRequired = !savedAppointment.ReservedTimeSlot.Equals(updatedAppointment.ReservedTimeSlot);
        await using var connection = await _dataSource.OpenConnectionAsync();
        var transaction = await connection.BeginTransactionAsync();

        if (isTimeSlotUpdateRequired)
        {
            await _timeSlotRepository.RemoveReservation(connection, savedAppointment.ReservedTimeSlotId);
            var reservedTimeSlotId =
                await _timeSlotRepository.ReserveTimeSlot(connection, updatedAppointment.ReservedTimeSlot);
            updatedAppointment.ReservedTimeSlotId = reservedTimeSlotId;
        }

        var appointmentSqlWithParams = _appointmentSqlRepresentation.ApplyFilter(_updateAppointmentStatementTemplate,
            x => x.AppointmentId == updatedAppointment.AppointmentId);
        await connection.ExecuteAsync(appointmentSqlWithParams.Sql, appointmentSqlWithParams.Parameters);
        await transaction.CommitAsync();
    }
}