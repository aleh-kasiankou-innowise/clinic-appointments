using System.Text;
using Dapper;
using Innowise.Clinic.Appointments.Persistence.EntityFilters.TimeSlots;
using Innowise.Clinic.Appointments.Persistence.Models;
using Innowise.Clinic.Appointments.Persistence.ObjectRelationalMapping;
using Innowise.Clinic.Appointments.Persistence.Repositories.Interfaces;
using Npgsql;

namespace Innowise.Clinic.Appointments.Persistence.Repositories.Implementations;

public class TimeSlotRepository : ITimeSlotRepository
{
    private readonly SqlRepresentation<ReservedTimeSlot> _timeSlotTableSqlRepresentation;
    private readonly string _insertTimeSlotStatement;
    private readonly string _deleteTimeSlotStatement;


    public TimeSlotRepository(SqlRepresentation<ReservedTimeSlot> sqlRepresentation)
    {
        _timeSlotTableSqlRepresentation = sqlRepresentation;

        var insertTimeSlotStatementTemplate = new StringBuilder(
            $"INSERT INTO {_timeSlotTableSqlRepresentation}({SqlVariables.InsertFields}) " +
            $"VALUES({SqlVariables.InsertValues}) " +
            $"RETURNING {_timeSlotTableSqlRepresentation.Property(x => x.ReservedTimeSlotId)};");

        var _updateTimeSlotStatementTemplate = new StringBuilder(
            $"UPDATE {_timeSlotTableSqlRepresentation} " +
            $"SET {SqlVariables.UpdateValues} " +
            $"WHERE {SqlVariables.Filter};");

        _deleteTimeSlotStatement =
            $"DELETE FROM {_timeSlotTableSqlRepresentation} " +
            $"WHERE {SqlVariables.Filter};";

        _insertTimeSlotStatement = sqlRepresentation.CompleteInsertStatement(insertTimeSlotStatementTemplate, false);
    }

    public async Task<Guid> ReserveTimeSlot(NpgsqlConnection connection, ReservedTimeSlot newTimeSlot)
    {
        var appointmentSqlParams = _timeSlotTableSqlRepresentation.MapPropertiesToSqlParameters(newTimeSlot);
        var id = await connection.ExecuteScalarAsync<Guid>(_insertTimeSlotStatement, appointmentSqlParams);
        return id;
    }

    public async Task RemoveReservation(NpgsqlConnection connection, Guid timeSlotId)
    {
        var sqlWithParams = _timeSlotTableSqlRepresentation.ApplyFilter(_deleteTimeSlotStatement,
            new IdFilter().ToExpression(timeSlotId.ToString()));
        await connection.ExecuteAsync(_deleteTimeSlotStatement);
    }
}