using System.Linq.Expressions;
using System.Text;
using Dapper;
using Innowise.Clinic.Appointments.Persistence.EntityFilters.Doctors;
using Innowise.Clinic.Appointments.Persistence.Models;
using Innowise.Clinic.Appointments.Persistence.ObjectRelationalMapping;
using Innowise.Clinic.Appointments.Persistence.Repositories.Interfaces;
using Innowise.Clinic.Shared.MassTransit.MessageTypes.Events;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Innowise.Clinic.Appointments.Persistence.Repositories.Implementations;

public class DoctorRepository : IDoctorRepository
{
    private readonly NpgsqlDataSource _dataSource;
    private readonly string _selectStatement;
    private readonly string _updateTemplate;
    private readonly string _insertStatement;
    private readonly SqlRepresentation<Doctor> _sqlRepresentation;
    private readonly ILogger<DoctorRepository> _logger;

    public DoctorRepository(NpgsqlDataSource dataSource,
        SqlRepresentation<Doctor> sqlRepresentation, ILogger<DoctorRepository> logger)
    {
        _dataSource = dataSource;
        _sqlRepresentation = sqlRepresentation;
        _logger = logger;
        _selectStatement = ($"SELECT * FROM {_sqlRepresentation} WHERE {SqlVariables.Filter};");
        var insertTemplate = new StringBuilder($"INSERT INTO {sqlRepresentation}({SqlVariables.InsertFields}) " +
                                               $"VALUES({SqlVariables.InsertValues}) " +
                                               $"RETURNING {sqlRepresentation.Property(x => x.DoctorId)};");
        var updateTemplate = new StringBuilder(
            $"UPDATE {_sqlRepresentation} " +
            $"SET {SqlVariables.UpdateValues} " +
            $"WHERE {SqlVariables.Filter};");

        _insertStatement = _sqlRepresentation.CompleteInsertStatement(insertTemplate, false);
        _updateTemplate = _sqlRepresentation.CompleteUpdateStatement(updateTemplate);
        _logger.LogDebug("Doctor repository has been initiated");
        _logger.LogDebug("Doctor repository SELECT statement: {Statement}", _selectStatement);
        _logger.LogDebug("Doctor repository INSERT statement: {Statement}", _insertStatement);
        _logger.LogDebug("Doctor repository UPDATE template: {Statement}", _updateTemplate);
    }

    public async Task<Doctor?> GetDoctorAsync(Expression<Func<Doctor, bool>> filter)
    {
        var sqlWithParams =
            _sqlRepresentation.ApplyFilter(_selectStatement, filter);
        await using var connection = await _dataSource.OpenConnectionAsync();
        return (await connection.QueryAsync<Doctor>(sqlWithParams.Sql
            , sqlWithParams.Parameters)).FirstOrDefault();
    }

    public async Task AddOrUpdateDoctorAsync(DoctorAddedOrUpdatedMessage doctorAddedOrUpdatedMessage)
    {
        var idFilter = new IdFilter().ToExpression(doctorAddedOrUpdatedMessage.DoctorId.ToString());
        var savedDoctor = await GetDoctorAsync(idFilter);
        await using var connection = await _dataSource.OpenConnectionAsync();
        var transaction = await connection.BeginTransactionAsync();

        if (savedDoctor is not null)
        {
            await UpdateDoctorAsync(connection);
        }
        else
        {
            await AddDoctorAsync(connection);
        }

        await transaction.CommitAsync();


        async Task UpdateDoctorAsync(NpgsqlConnection npgsqlConnection)
        {
            var appointmentSqlWithParams = _sqlRepresentation.ApplyFilter(_updateTemplate,
                idFilter);
            var sqlParameters =
                _sqlRepresentation.MapPropertiesToSqlParameters(ConvertMessageToDoctor(),
                    appointmentSqlWithParams.Parameters);
            await npgsqlConnection.ExecuteAsync(appointmentSqlWithParams.Sql, sqlParameters);
        }

        async Task AddDoctorAsync(NpgsqlConnection npgsqlConnection)
        {
            var doctorSqlParams = _sqlRepresentation.MapPropertiesToSqlParameters(ConvertMessageToDoctor());
            await npgsqlConnection.ExecuteAsync(_insertStatement, doctorSqlParams);
        }

        Doctor ConvertMessageToDoctor()
        {
            return new()
            {
                DoctorId = doctorAddedOrUpdatedMessage.DoctorId,
                SpecializationId = doctorAddedOrUpdatedMessage.SpecializationId,
                OfficeId = doctorAddedOrUpdatedMessage.OfficeId
            };
        }
    }
}