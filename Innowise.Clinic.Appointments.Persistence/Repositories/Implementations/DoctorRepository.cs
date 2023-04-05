using System.Linq.Expressions;
using System.Text;
using Dapper;
using Innowise.Clinic.Appointments.Persistence.Models;
using Innowise.Clinic.Appointments.Persistence.Repositories.Interfaces;
using Innowise.Clinic.Shared.Services.FiltrationService;
using Innowise.Clinic.Shared.Services.SqlMappingService;
using Npgsql;

namespace Innowise.Clinic.Appointments.Persistence.Repositories.Implementations;

public class DoctorRepository : IDoctorRepository
{
    private readonly NpgsqlDataSource _dataSource;
    private readonly string _selectStatement;
    private readonly SqlRepresentation<Doctor> _sqlRepresentation;

    public DoctorRepository(NpgsqlDataSource dataSource, TreeToSqlVisitor toSqlVisitor, ISqlMapper sqlMapper,
        SqlRepresentation<Doctor> sqlRepresentation)
    {
        _dataSource = dataSource;
        _sqlRepresentation = sqlRepresentation;
        _selectStatement = ($"SELECT * FROM {_sqlRepresentation} WHERE [FILTER];");
    }

    public async Task<Doctor> GetDoctorAsync(Expression<Func<Doctor, bool>> filter)
    {
        var sqlWithParams =
            _sqlRepresentation.ApplyFilter(_selectStatement, filter);
        await using var connection = await _dataSource.OpenConnectionAsync();
        return await connection.QuerySingleAsync<Doctor>(sqlWithParams.Sql
            , sqlWithParams.Parameters);
    }
}