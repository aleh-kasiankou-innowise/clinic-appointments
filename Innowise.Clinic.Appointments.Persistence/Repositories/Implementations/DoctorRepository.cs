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

        _insertStatement = _sqlRepresentation.CompleteInsertStatement(insertTemplate);
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
        return await connection.QueryFirstOrDefaultAsync<Doctor>(sqlWithParams.Sql
            , sqlWithParams.Parameters);
    }

    public async Task AddOrUpdateDoctorAsync(DoctorAddedOrUpdatedMessage doctorAddedOrUpdatedMessage)
    {
        var idFilter = new IdFilter().ToExpression(doctorAddedOrUpdatedMessage.DoctorId.ToString());
        var savedDoctor = await GetDoctorAsync(idFilter);
        if (savedDoctor is not null)
        {
            await UpdateDoctorAsync();
        }
        else
        {
            await AddDoctorAsync();
        }

        
        async Task UpdateDoctorAsync()
        {
            var appointmentSqlWithParams = _sqlRepresentation.ApplyFilter(_updateTemplate,
                idFilter);
            var sqlParameters = _sqlRepresentation.MapPropertiesToSqlParameters(ConvertMessageToDoctor(), appointmentSqlWithParams.Parameters);
            await using var connection = await _dataSource.OpenConnectionAsync();
            await connection.ExecuteAsync(appointmentSqlWithParams.Sql, sqlParameters);
        }

        async Task AddDoctorAsync()
        {
            var doctorSqlParams = _sqlRepresentation.MapPropertiesToSqlParameters(ConvertMessageToDoctor());
            await using var connection = await _dataSource.OpenConnectionAsync();
            await connection.ExecuteAsync(_insertStatement, doctorSqlParams);
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