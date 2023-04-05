using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using Innowise.Clinic.Shared.BaseClasses;
using Innowise.Clinic.Shared.Services.FiltrationService;
using Innowise.Clinic.Shared.Services.SqlMappingService;

namespace Innowise.Clinic.Appointments.Persistence;

public abstract class SqlRepresentation<T> where T : IEntity
{
    private readonly TreeToSqlVisitor _toSqlVisitor;
    private readonly ISqlMapper _sqlMapper;

    private readonly IDictionary<PropertyInfo, (string param, string sqlName)> _props =
        new Dictionary<PropertyInfo, (string, string)>();

    public SqlRepresentation(TreeToSqlVisitor toSqlVisitor, ISqlMapper sqlMapper)
    {
        _toSqlVisitor = toSqlVisitor;
        _sqlMapper = sqlMapper;
        CacheTypeProps();
    }

    public string Property<TProp>(Expression<Func<T, TProp>> propertyExpression, bool excludeTableName = false)
    {
        // TODO WON'T WORK FOR NESTED PROPERTIES (that represent another entity)
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
        var sqlParamValues = new Dictionary<string, object>();
        var whereClause = _toSqlVisitor.Visit(filter, typeof(T), sqlParamValues);
        var finalStatement = statement.Replace("[FILTER]", whereClause.ToString());
        return (finalStatement, sqlParamValues);
    }

    public Dictionary<string, object> MapPropertiesToSqlParameters(
        T instance)
    {
        var paramDictionary = new Dictionary<string, object>();
        foreach (var keyValuePair in _props)
        {
            paramDictionary.Add(keyValuePair.Value.param, GetPropertyValue(keyValuePair.Key, instance));
        }

        return paramDictionary;
    }

    public string CompleteInsertStatement(StringBuilder insertStatementTemplate)
    {
        var commaSeparatedFields = string.Join(", ", _props.Values.Select(x => x.sqlName));
        var commaSeparatedParams = string.Join(", ", _props.Values.Select(x => x.param));
        var replace = insertStatementTemplate
            .Replace("[FIELDS]", commaSeparatedFields)
            .Replace("[VALUES]", commaSeparatedParams);
        return replace.ToString();
    }

    public string CompleteUpdateStatement(StringBuilder updateStatementTemplate)
    {
        var mappingWithParams = new StringBuilder();
        foreach (var keyValuePair in _props)
        {
            mappingWithParams.Append($"{keyValuePair.Value.sqlName} = {keyValuePair.Value.param}");
        }

        return updateStatementTemplate.Replace("[UPDATEMAPPINGS]", mappingWithParams.ToString()).ToString();
    }

    private void CacheTypeProps()
    {
        var properties = typeof(T).GetProperties();
        foreach (var prop in properties)
        {
            // TODO ENCAPSULATE LOGIC OF CHECKING ID PROPERTY INTO MAPPER

            if (!prop.GetMethod.IsVirtual && !prop.Name.EndsWith("id", StringComparison.OrdinalIgnoreCase))
            {
                var sqlPropName = _sqlMapper.GetSqlPropertyName(typeof(T), prop);
                _props.Add(prop, ("@" + sqlPropName, sqlPropName));
            }
        }
    }

    private object GetPropertyValue(PropertyInfo prop, T instance)
    {
        return ((Func<object>)Delegate.CreateDelegate(typeof(T), instance, prop.GetMethod.Name))();
    }
}