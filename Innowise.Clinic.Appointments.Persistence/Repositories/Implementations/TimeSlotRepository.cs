using System.Text;
using Dapper;
using Innowise.Clinic.Appointments.Persistence.EntityFilters.TimeSlots;
using Innowise.Clinic.Appointments.Persistence.Models;
using Innowise.Clinic.Appointments.Persistence.Repositories.Interfaces;
using Npgsql;

namespace Innowise.Clinic.Appointments.Persistence.Repositories.Implementations;

// TODO CONSIDER ADDING UPDATE STATEMENT
public class TimeSlotRepository : ITimeSlotRepository
{
    private SqlRepresentation<ReservedTimeSlot> _timeSlotTableSqlRepresentation;
    private readonly string _insertTimeSlotStatement;
    private readonly string _updateTimeSlotStatement;
    private readonly string _deleteTimeSlotStatement;


    public TimeSlotRepository(SqlRepresentation<ReservedTimeSlot> sqlRepresentation)
    {
        _timeSlotTableSqlRepresentation = sqlRepresentation;

        var insertTimeSlotStatementTemplate = new StringBuilder(
            $"INSERT INTO {_timeSlotTableSqlRepresentation}([FIELDS]) " +
            $"VALUES(gen_random_uuid(), [VALUES]);");

        var _updateTimeSlotStatementTemplate = new StringBuilder(
            $"UPDATE {_timeSlotTableSqlRepresentation} " +
            $"SET [UPDATEMAPPINGS] " +
            "WHERE [FILTER];");

        _deleteTimeSlotStatement =
            $"DELETE FROM {_timeSlotTableSqlRepresentation} " +
            $"WHERE [FILTER];";

        _insertTimeSlotStatement = sqlRepresentation.CompleteInsertStatement(insertTimeSlotStatementTemplate);
        _updateTimeSlotStatement = sqlRepresentation.CompleteUpdateStatement(_updateTimeSlotStatementTemplate);
    }

    public async Task<Guid> ReserveTimeSlot(NpgsqlConnection connection, ReservedTimeSlot newTimeSlot)
    {
        var appointmentSqlParams = _timeSlotTableSqlRepresentation.MapPropertiesToSqlParameters(newTimeSlot);
        var id = await connection.QueryFirstAsync<Guid>(_insertTimeSlotStatement, appointmentSqlParams);
        return id;
    }

    public async Task RemoveReservation(NpgsqlConnection connection, Guid timeSlotId)
    {
        var sqlWithParams = _timeSlotTableSqlRepresentation.ApplyFilter(_deleteTimeSlotStatement,
            new IdFilter().ToExpression(timeSlotId.ToString()));
        await connection.ExecuteAsync(_deleteTimeSlotStatement);
    }
}