using System.Linq.Expressions;
using System.Text;
using Dapper;
using Innowise.Clinic.Appointments.Persistence.Models;
using Innowise.Clinic.Appointments.Persistence.ObjectRelationalMapping;
using Innowise.Clinic.Appointments.Persistence.Repositories.Interfaces;
using Innowise.Clinic.Shared.Exceptions;
using Microsoft.Extensions.Logging;
using Npgsql;
using IdFilter = Innowise.Clinic.Appointments.Persistence.EntityFilters.AppointmentResults.IdFilter;

namespace Innowise.Clinic.Appointments.Persistence.Repositories.Implementations;

public class AppointmentResultsRepository : IAppointmentResultsRepository
{
    private readonly NpgsqlDataSource _dataSource;
    private readonly SqlRepresentation<AppointmentResult> _sqlRepresentation;
    private readonly SqlRepresentation<Appointment> _appointmentSqlRepresentation;
    private readonly SqlRepresentation<Doctor> _doctorSqlRepresentation;
    private readonly ILogger<AppointmentResultsRepository> _logger;
    private readonly string _selectStatement;
    private readonly string _insertStatement;
    private readonly string _updateStatement;

    public AppointmentResultsRepository(NpgsqlDataSource dataSource,
        SqlRepresentation<AppointmentResult> sqlRepresentation,
        SqlRepresentation<Appointment> appointmentSqlRepresentation,
        SqlRepresentation<ReservedTimeSlot> timeslotSqlRepresentation,
        ILogger<AppointmentResultsRepository> logger, SqlRepresentation<Doctor> doctorSqlRepresentation)
    {
        _dataSource = dataSource;
        _sqlRepresentation = sqlRepresentation;
        _appointmentSqlRepresentation = appointmentSqlRepresentation;
        _logger = logger;
        _doctorSqlRepresentation = doctorSqlRepresentation;

        _selectStatement =
            $"SELECT * FROM {sqlRepresentation} " +
            $"INNER JOIN {_appointmentSqlRepresentation} " +
            $"ON {sqlRepresentation.Property(x => x.AppointmentId)} = " +
            $"{appointmentSqlRepresentation.Property(x => x.AppointmentId)} " +
            $"INNER JOIN {timeslotSqlRepresentation} " +
            $"ON {_appointmentSqlRepresentation.Property(x => x.ReservedTimeSlotId)} = " +
            $"{timeslotSqlRepresentation.Property(x => x.ReservedTimeSlotId)} " +
            $"INNER JOIN {_doctorSqlRepresentation} ON " +
            $"{_appointmentSqlRepresentation.Property(x => x.DoctorId)} = {_doctorSqlRepresentation.Property(x => x.DoctorId)} " +
            $"WHERE {SqlVariables.Filter};";

        var insertStatementTemplate =
            new StringBuilder(
                $"INSERT INTO {_sqlRepresentation}({SqlVariables.InsertFields}) VALUES({SqlVariables.InsertValues}) " +
                $"RETURNING {sqlRepresentation.Property(x => x.AppointmentResultId)};");

        _insertStatement = _sqlRepresentation.CompleteInsertStatement(insertStatementTemplate);
        _updateStatement = $"UPDATE {_sqlRepresentation} SET {SqlVariables.UpdateValues} WHERE {SqlVariables.Filter};";
    }

    public async Task<AppointmentResult> GetAppointmentResultAsync(Expression<Func<AppointmentResult, bool>> filter)
    {
        try
        {
            var sqlWithParams =
                _sqlRepresentation.ApplyFilter(_selectStatement, filter);
            await using var connection = await _dataSource.OpenConnectionAsync();
            return (await connection.QueryAsync<AppointmentResult, Appointment, ReservedTimeSlot, Doctor, AppointmentResult>(
                       sqlWithParams.Sql
                       , (appointmentResult, appointment, timeslot, doctor) =>
                       {
                           appointmentResult.Appointment = appointment;
                           appointmentResult.Appointment.ReservedTimeSlot = timeslot;
                           appointmentResult.Appointment.Doctor = doctor;
                           return appointmentResult;
                       },
                       sqlWithParams.Parameters,
                       splitOn:
                       $"{_sqlRepresentation.Property(x => x.AppointmentId, true)}, " +
                       $"{_appointmentSqlRepresentation.Property(x => x.ReservedTimeSlotId, true)}, " +
                       $"{_doctorSqlRepresentation.Property(x => x.DoctorId, true)}"))
                   .SingleOrDefault() ??
                   throw new EntityNotFoundException("Cannot find appointment result that meets the filter criteria.");
        }

        catch
        {
            _logger.LogWarning("Cannot find {Type} that meets the following criteria {Criteria}",
                typeof(AppointmentResult), filter);
            throw;
        }
    }

    public async Task<Guid> CreateAppointmentResultAsync(AppointmentResult newAppointmentResult)
    {
        var appointmentSqlParams = _sqlRepresentation.MapPropertiesToSqlParameters(newAppointmentResult);
        await using var connection = await _dataSource.OpenConnectionAsync();
        var transaction = await connection.BeginTransactionAsync();
        var id = await connection.ExecuteScalarAsync<Guid>(_insertStatement, appointmentSqlParams);
        await transaction.CommitAsync();
        return id;
    }

    public async Task UpdateAppointmentResultAsync(AppointmentResult appointmentResult)
    {
        var idFilterExpression = new IdFilter().ToExpression(appointmentResult.AppointmentResultId.ToString());
        var appointmentSqlWithParams = _sqlRepresentation.ApplyFilter(_updateStatement, idFilterExpression);
        await using var connection = await _dataSource.OpenConnectionAsync();
        await connection.ExecuteAsync(appointmentSqlWithParams.Sql, appointmentSqlWithParams.Parameters);
    }
}