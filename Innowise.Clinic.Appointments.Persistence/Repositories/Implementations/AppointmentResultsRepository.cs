using System.Linq.Expressions;
using System.Text;
using Dapper;
using Innowise.Clinic.Appointments.Persistence.Models;
using Innowise.Clinic.Appointments.Persistence.Repositories.Interfaces;
using Npgsql;
using IdFilter = Innowise.Clinic.Appointments.Persistence.EntityFilters.AppointmentResults.IdFilter;

// TODO MOVE TEMPLATE VARIABLES TO STATIC CLASS
namespace Innowise.Clinic.Appointments.Persistence.Repositories.Implementations;

public class AppointmentResultsRepository : IAppointmentResultsRepository
{
    private readonly NpgsqlDataSource _dataSource;
    private readonly SqlRepresentation<AppointmentResult> _sqlRepresentation;
    private readonly string _selectStatement;
    private readonly string _insertStatement;
    private readonly string _updateStatement;

    public AppointmentResultsRepository(NpgsqlDataSource dataSource,
        SqlRepresentation<AppointmentResult> sqlRepresentation)
    {
        _dataSource = dataSource;
        _sqlRepresentation = sqlRepresentation;

        _selectStatement = $"SELECT * FROM {sqlRepresentation} WHERE [FILTER];";

        var insertStatementTemplate =
            new StringBuilder(
                $"INSERT INTO {_sqlRepresentation}([FIELDS]) VALUES(gen_random_uuid(), [VALUES]) " +
                $"RETURNING {sqlRepresentation.Property(x => x.AppointmentResultId)};");

        _insertStatement = _sqlRepresentation.CompleteInsertStatement(insertStatementTemplate);

        _updateStatement = "UPDATE {tableName} SET [UPDATEMAPPINGS] WHERE [FILTER];";
    }

    public async Task<AppointmentResult> GetAppointmentResultAsync(Expression<Func<AppointmentResult, bool>> filter)
    {
        var sqlWithParams =
            _sqlRepresentation.ApplyFilter(_selectStatement, filter);
        await using var connection = await _dataSource.OpenConnectionAsync();
        return await connection.QuerySingleAsync<AppointmentResult>(sqlWithParams.Sql
            , sqlWithParams.Parameters);
    }

    public async Task<Guid> CreateAppointmentResultAsync(AppointmentResult newAppointmentResult)
    {
        // TODO check whether need to link to appointment manually
        var appointmentSqlParams = _sqlRepresentation.MapPropertiesToSqlParameters(newAppointmentResult);
        await using var connection = await _dataSource.OpenConnectionAsync();
        var transaction = await connection.BeginTransactionAsync();
        var id = await connection.QueryFirstAsync<Guid>(_insertStatement, appointmentSqlParams);
        await transaction.CommitAsync();
        return id;
    }

    public async Task UpdateAppointmentResultAsync(AppointmentResult appointmentResult)
    {
        var savedAppointment =
            await GetAppointmentResultAsync(
                new IdFilter().ToExpression(appointmentResult.AppointmentResultId.ToString()));
        var appointmentSqlWithParams = _sqlRepresentation.ApplyFilter(_updateStatement,
            new IdFilter().ToExpression(appointmentResult.AppointmentResultId.ToString()));
        await using var connection = await _dataSource.OpenConnectionAsync();
        await connection.ExecuteAsync(appointmentSqlWithParams.Sql, appointmentSqlWithParams.Parameters);
    }
}