using System.Data;
using System.Globalization;
using Dapper;

namespace Foodblog.DataAccess.Sqlite;

public static class DapperTypeHandlers
{
    private static readonly object Lock = new();
    private static bool _registered;

    public static void Register()
    {
        lock (Lock)
        {
            if (_registered)
            {
                return;
            }

            DefaultTypeMap.MatchNamesWithUnderscores = true;
            SqlMapper.AddTypeHandler(new DateTimeOffsetHandler());
            SqlMapper.AddTypeHandler(new DecimalHandler());
            _registered = true;
        }
    }

    private sealed class DateTimeOffsetHandler : SqlMapper.TypeHandler<DateTimeOffset>
    {
        public override DateTimeOffset Parse(object value) =>
            DateTimeOffset.Parse((string)value, CultureInfo.InvariantCulture);

        public override void SetValue(IDbDataParameter parameter, DateTimeOffset value) =>
            parameter.Value = value.ToString("O", CultureInfo.InvariantCulture);
    }

    private sealed class DecimalHandler : SqlMapper.TypeHandler<decimal>
    {
        public override decimal Parse(object value) => Convert.ToDecimal(value);

        public override void SetValue(IDbDataParameter parameter, decimal value)
        {
            parameter.Value = (double)value;
            parameter.DbType = DbType.Double;
        }
    }
}
