using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using Innowise.Clinic.Appointments.Persistence.Models;
using Innowise.Clinic.Shared.BaseClasses;
using Innowise.Clinic.Shared.Services.FiltrationService;
using Innowise.Clinic.Shared.Services.SqlMappingService;
using Microsoft.Extensions.Logging;

namespace Innowise.Clinic.Appointments.Persistence.ObjectRelationalMapping;

public class SqlRepresentation<T> where T : IEntity
{
    private readonly TreeToSqlVisitor _toSqlVisitor;
    private readonly ISqlMapper _sqlMapper;

    private readonly IDictionary<PropertyInfo, (string param, string sqlName)> _props =
        new Dictionary<PropertyInfo, (string, string)>();

    private readonly ILogger<SqlRepresentation<T>> _logger;

    public SqlRepresentation(TreeToSqlVisitor toSqlVisitor, ISqlMapper sqlMapper, ILogger<SqlRepresentation<T>> logger)
    {
        _toSqlVisitor = toSqlVisitor;
        _sqlMapper = sqlMapper;
        _logger = logger;
        CacheTypeProps();
    }

    public string Property<TProp>(Expression<Func<T, TProp>> propertyExpression, bool excludeTableName = false)
    {
        var propertyExpressionBody = (MemberExpression)propertyExpression.Body;
        var tableName = excludeTableName ? "" : string.Concat("\"", _sqlMapper.GetSqlTableName(typeof(T)), "\".");
        var property = _sqlMapper.GetSqlPropertyName(typeof(T), (PropertyInfo)propertyExpressionBody.Member);
        return string.IsNullOrWhiteSpace(tableName) ? property : string.Concat(tableName, $"\"{property}\"");
    }

    public override string ToString()
    {
        return $"\"{_sqlMapper.GetSqlTableName(typeof(T))}\"";
    }

    public static implicit operator string(SqlRepresentation<T> sqlRepresentation)
    {
        return sqlRepresentation.ToString();
    }

    public (string Sql, Dictionary<string, object> Parameters) ApplyFilter(string statement,
        Expression<Func<T, bool>> filter)
    {
        try
        {
            var sqlParamValues = new Dictionary<string, object>();
            var whereClause = _toSqlVisitor.Visit(filter, typeof(T), sqlParamValues);
            var finalStatement = statement.Replace(SqlVariables.Filter, whereClause.ToString());
            return (finalStatement, sqlParamValues);
        }

        catch
        {
            _logger.LogWarning("Cannot apply filter to type {Type} : {Filter}\n Statement: {Statement}", typeof(T).Name,
                filter, statement);
            throw;
        }
    }

    public Dictionary<string, object> MapPropertiesToSqlParameters(
        T instance)
    {
        var paramDictionary = new Dictionary<string, object>();
        return MapPropertiesToSqlParameters(instance, paramDictionary);
    }

    public Dictionary<string, object> MapPropertiesToSqlParameters(
        T instance, Dictionary<string, object> parameters)
    {
        foreach (var keyValuePair in _props)
        {
            var propertyValue = GetPropertyValue(keyValuePair.Key, instance);
            parameters.Add(keyValuePair.Value.param, propertyValue);
            _logger.LogDebug("Mapping {Param} to {ParamValue}", keyValuePair.Value.param, propertyValue);
        }

        return parameters;
    }

    public string CompleteInsertStatement(StringBuilder insertStatementTemplate, bool generateRandomGuid = true)
    {
        var commaSeparatedFields = string.Join(", ", _props.Values.Select(x => $"\"{x.sqlName}\""));
        var paramPosition = 0;
        // TODO USE ATTRIBUTES TO DETERMINE ID FIELD INSTEAD OF USING POSITION IN A DICTIONARY
        var commaSeparatedParams = string.Join(", ",
            _props.Values.Select(x => paramPosition++ == 0 && generateRandomGuid ? "gen_random_uuid()" : x.param));
        var replace = insertStatementTemplate
            .Replace(SqlVariables.InsertFields, commaSeparatedFields)
            .Replace(SqlVariables.InsertValues, commaSeparatedParams);
        return replace.ToString();
    }

    public string CompleteUpdateStatement(StringBuilder updateStatementTemplate)
    {
        var mappingWithParams = new StringBuilder();
        foreach (var keyValuePair in _props)
        {
            mappingWithParams.Append(
                $"\"{keyValuePair.Value.sqlName}\" = {keyValuePair.Value.param},");
            _logger.LogDebug("\"{Table}.{Column}\" = {Param},", _sqlMapper.GetSqlTableName(typeof(T)),
                keyValuePair.Value.sqlName, keyValuePair.Value.param);
        }

        // removing comma
        mappingWithParams.Remove(mappingWithParams.Length - 1, 1);

        return updateStatementTemplate.Replace(SqlVariables.UpdateValues, mappingWithParams.ToString()).ToString();
    }

    private void CacheTypeProps()
    {
        var properties = typeof(T).GetProperties();
        _logger.LogDebug("Caching table properties for {Type}", typeof(T).Name);
        foreach (var prop in properties)
        {
            if (!prop.GetMethod.IsVirtual)
            {
                var sqlPropName = _sqlMapper.GetSqlPropertyName(typeof(T), prop);
                var parameter = "@" + sqlPropName;
                _props.Add(prop, (parameter, sqlPropName));
                _logger.LogDebug("Caching parameter {Parameter}", parameter);
            }
        }
    }

    private object? GetPropertyValue(PropertyInfo prop, T instance)
    {
        return prop.GetValue(instance);
    }
}